using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WTExpCalc.Models;
using WTExpCalc.Services.Api;
using WTExpCalc.Services.Localization;
using WTExpCalc.Services.Settings;
using WTExpCalc.Services.Tree;
using WTExpCalc.Services.UrlState;
using WTExpCalc.Shared.Localization;

namespace WTExpCalc.Shared.Tree
{
    public partial class TechTree
    {
        [Parameter] public int NationId { get; set; }
        [Parameter] public int TypeId { get; set; }
        [Parameter] public long? RpLimit { get; set; }
        [Parameter] public EventCallback<TotalsUpdateEventArgs> OnTotalsUpdated { get; set; }
        [Parameter] public EventCallback<List<int>> OnSelectedRanksChanged { get; set; }
        [Parameter] public EventCallback<string> ScreenshotProgress { get; set; }
        [Parameter] public EventCallback<ScreenshotResult> ScreenshotCompleted { get; set; }

        private enum TreeLoadState
        {
            Loading,
            Loaded,
            Error
        }

        private List<Node> AllNodesFlat = new();
        private Dictionary<int, Node> NodeLookup = new();
        private TreeLayout _layout = TreeLayout.Empty;
        private List<NodeDependency> Dependencies = new();
        private Node? currentlyExpandedFolder;
        private TreeLoadState LoadingState = TreeLoadState.Loading;
        private string? _loadErrorMessage;
        private List<RankRequirement> rankRequirements = new();
        private TreeSelectionState _selection = new(
            new Dictionary<int, Node>(),
            new Dictionary<int, int>());

        private string LoadingStateCss => LoadingState switch
        {
            TreeLoadState.Loading => "loading",
            TreeLoadState.Error => "error",
            _ => "loaded"
        };

        // Исходные значения RP из API.
        private Dictionary<int, int> originalRpValues = new();
        // Флаг для предотвращения циклических обновлений URL
        private bool _suppressUrlUpdate = false;

        private int _previousNationId = -1;
        private int _previousTypeId = -1;
        private long? _previousRpLimit;
        private bool _isFirstLoad = true;

        private long? maxRpLimit = null;

        private IReadOnlyList<Node> GridNodes => _layout.GridNodes;
        private string GridStyles => _layout.GridStyles;
        private IReadOnlyDictionary<int, int> MaxRowIndexPerRank => _layout.MaxRowIndexPerRank;

        private bool _deferConnectionRedrawAfterRender;
        private bool _deferFolderPopupPositionAfterRender;
        private DotNetObjectReference<TechTree>? _layoutInteropRef;
        private bool _treeLayoutListenersAttached;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            bool wantLines = _deferConnectionRedrawAfterRender &&
                             LoadingState == TreeLoadState.Loaded && Dependencies.Any();
            bool wantPopup = _deferFolderPopupPositionAfterRender &&
                             LoadingState == TreeLoadState.Loaded && currentlyExpandedFolder != null;

            if (wantLines || wantPopup)
            {
                await WaitDomLayoutStableAsync();

                if (wantLines)
                {
                    _deferConnectionRedrawAfterRender = false;
                    await DrawConnectionLines();
                }

                if (wantPopup && currentlyExpandedFolder != null)
                {
                    _deferFolderPopupPositionAfterRender = false;
                    try
                    {
                        await JSRuntime.InvokeVoidAsync("techTreeFunctions.positionFolderPopup",
                            $"folder-popup-{currentlyExpandedFolder.Id}",
                            $"node-{currentlyExpandedFolder.Id}");
                    }
                    catch
                    {
                        /* Popup may be gone if user navigates away mid-frame */
                    }
                }
            }

            await EnsureTreeLayoutObserversAsync();

            try
            {
                await JSRuntime.InvokeVoidAsync("techTreeFunctions.syncSummaryPosition");
            }
            catch
            {
                /* Summary bar may not be rendered yet */
            }
        }

        private async Task WaitDomLayoutStableAsync()
        {
            if (JSRuntime == null)
            {
                await Task.Delay(24);
                return;
            }

            try
            {
                await JSRuntime.InvokeAsync<bool>("techTreeFunctions.waitTwoAnimationFramesAsync");
            }
            catch
            {
                await Task.Delay(24);
            }
        }

        /// <summary>Queues dependency-line redraw after DOM updates (opening a folder intentionally does not call this).</summary>
        private void ScheduleConnectionRedraw()
        {
            if (LoadingState != TreeLoadState.Loaded || Dependencies.Count == 0) return;
            _deferConnectionRedrawAfterRender = true;
        }

        private async Task TeardownTreeLayoutInteropAsync()
        {
            _treeLayoutListenersAttached = false;
            if (JSRuntime != null)
            {
                try { await JSRuntime.InvokeVoidAsync("techTreeFunctions.disconnectTechTreeLayout"); }
                catch { /* ignore */ }
            }

            _layoutInteropRef?.Dispose();
            _layoutInteropRef = null;
        }

        private async Task EnsureTreeLayoutObserversAsync()
        {
            if (_treeLayoutListenersAttached) return;
            if (JSRuntime == null || LoadingState != TreeLoadState.Loaded || Dependencies.Count == 0) return;

            try
            {
                _layoutInteropRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync(
                    "techTreeFunctions.observeTechTreeLayout",
                    "tech-tree-container-id",
                    _layoutInteropRef,
                    110);
                _treeLayoutListenersAttached = true;
            }
            catch
            {
                _treeLayoutListenersAttached = false;
                _layoutInteropRef?.Dispose();
                _layoutInteropRef = null;
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            // Подписываемся на изменение языка только один раз
            if (_isFirstLoad)
            {
                Localization.LanguageChanged += OnLanguageChanged;
            }

            var nationOrTypeChanged = _isFirstLoad || NationId != _previousNationId || TypeId != _previousTypeId;
            var limitChanged = RpLimit != _previousRpLimit;

            if (nationOrTypeChanged)
            {
                _previousNationId = NationId;
                _previousTypeId = TypeId;
                _previousRpLimit = RpLimit;
                _isFirstLoad = false;
                maxRpLimit = RpLimit;

                await LoadData();
            }
            else if (limitChanged)
            {
                _previousRpLimit = RpLimit;
                await ApplyRpLimitAsync(RpLimit);
            }
        }

        private async Task ApplyRpLimitAsync(long? limit)
        {
            maxRpLimit = limit;

            var removedSelection = false;

            // Если текущий опыт превышает лимит, снимаем выбор с последних узлов
            if (limit.HasValue && _selection.TotalRp > limit.Value)
            {
                removedSelection = _selection.RemoveExcess(limit.Value);
            }

            await CalculateAndUpdateTotals();

            if (removedSelection)
            {
                UpdateUrlState();
            }

            ScheduleConnectionRedraw();
            StateHasChanged();
        }

        private void OnLanguageChanged()
        {
            InvokeAsync(async () =>
            {
                await CalculateAndUpdateTotals();
                ScheduleConnectionRedraw();
                StateHasChanged();
            });
        }
        private string GetNodeDisplayName(Node node)
        {
            return Localization.NodeName(node.Name, node.NameEnglish);
        }

        private async Task LoadData()
        {
            try
            {
                await TeardownTreeLayoutInteropAsync();
                _deferConnectionRedrawAfterRender = false;
                _deferFolderPopupPositionAfterRender = false;

                LoadingState = TreeLoadState.Loading;
                _loadErrorMessage = null;
                _suppressUrlUpdate = true; // Предотвращаем обновление URL во время загрузки

                _layout = TreeLayout.Empty;
                Dependencies.Clear();
                rankRequirements.Clear();
                _selection.Clear();
                originalRpValues.Clear();
                currentlyExpandedFolder = null;
                AllNodesFlat = new();
                NodeLookup = new();
                await ResetTotalsAsync();
                StateHasChanged();

                AllNodesFlat = await Api.GetAllNodesFlatAsync(NationId, TypeId);
                if (AllNodesFlat == null || AllNodesFlat.Count == 0)
                {
                    _layout = TreeLayout.Empty;
                    LoadingState = TreeLoadState.Loaded;
                    _suppressUrlUpdate = false;
                    await ResetTotalsAsync();
                    StateHasChanged();
                    return;
                }

                // Сохраняем исходные значения RP
                foreach (var node in AllNodesFlat)
                {
                    if (node.RequiredExp.HasValue)
                    {
                        originalRpValues[node.Id] = node.RequiredExp.Value;
                    }
                }

                rankRequirements = await Api.GetRankRequirementsAsync(NationId, TypeId);
                NodeLookup = AllNodesFlat.ToDictionary(n => n.Id);
                _selection = new TreeSelectionState(NodeLookup, originalRpValues);
                _layout = TreeLayout.Build(AllNodesFlat, rankRequirements);
                Dependencies = await Api.GetAllDependenciesAsync(NodeLookup.Keys.ToList());

                // Восстанавливаем состояние из URL
                RestoreStateFromUrl();

                LoadingState = TreeLoadState.Loaded;
                _suppressUrlUpdate = false; // Разрешаем обновление URL

                await CalculateAndUpdateTotals();

                if (maxRpLimit.HasValue && _selection.TotalRp > maxRpLimit.Value)
                {
                    _selection.RemoveExcess(maxRpLimit.Value);
                    await CalculateAndUpdateTotals();
                    UpdateUrlState();
                }

                ScheduleConnectionRedraw();
            }
            catch (Exception ex)
            {
                _suppressUrlUpdate = false;
                _loadErrorMessage = Localization.Text(UIStrings.DataLoadError, ex.Message);
                LoadingState = TreeLoadState.Error;
                await ResetTotalsAsync();
            }
            StateHasChanged();
        }

        private async Task ResetTotalsAsync()
        {
            _selection.Clear();

            if (OnTotalsUpdated.HasDelegate)
            {
                await OnTotalsUpdated.InvokeAsync(new TotalsUpdateEventArgs
                {
                    TotalRp = 0,
                    TotalSl = 0
                });
            }

            if (OnSelectedRanksChanged.HasDelegate)
            {
                await OnSelectedRanksChanged.InvokeAsync(new List<int>());
            }
        }

        private async Task RetryLoad()
        {
            await LoadData();
        }

        private void RestoreStateFromUrl()
        {
            try
            {
                var urlState = UrlStateManager.RestoreSelectionFromUrl(AllNodesFlat, Navigation.Uri);

                _selection.Restore(urlState.SelectedNodeIds, urlState.ModifiedRpValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring state from URL: {ex.Message}");
            }
        }

        private void UpdateUrlState()
        {
            if (_suppressUrlUpdate || LoadingState != TreeLoadState.Loaded) return;

            try
            {
                UrlStateManager.UpdateUrlWithSelection(
                    AllNodesFlat,
                    _selection.SelectedIds,
                    originalRpValues,
                    _selection.RpOverrides,
                    UiSettings.Current.IncludePremiumInShareUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating URL state: {ex.Message}");
            }
        }
        private async Task CalculateAndUpdateTotals()
        {
            var allSelectedNodes = _selection.SelectedNodes;
            var namesString = string.Join(", ", allSelectedNodes.Select(GetNodeDisplayName));
            var formattedText = SummaryFormatter.Format(
                allSelectedNodes,
                originalRpValues,
                _selection.RpOverrides,
                _selection.TotalRp);

            if (OnTotalsUpdated.HasDelegate)
            {
                await OnTotalsUpdated.InvokeAsync(new TotalsUpdateEventArgs
                    {
                        TotalRp = _selection.TotalRp,
                        TotalSl = _selection.TotalSl,
                        SelectedNames = namesString,
                        FormattedText = formattedText
                    });
            }

            // Уведомляем родительский компонент об изменении выбранных рангов
            if (OnSelectedRanksChanged.HasDelegate)
            {
                await OnSelectedRanksChanged.InvokeAsync(_selection.SelectedRanks.ToList());
            }

            await UpdateNodesLimitStateAsync();
        }

        private async Task UpdateNodesLimitStateAsync()
        {
            if (JSRuntime == null)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync(
                    "techTreeFunctions.updateNodesLimitState",
                    maxRpLimit,
                    _selection.TotalRp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update nodes limit state: {ex.Message}");
            }
        }
        private string GetNodeGridStyle(Node node) => _layout.GetNodeGridStyle(node);

        private string GetNodeCssClass(Node node)
        {
            return $"tree-grid-item node-type-{node.Type} tech-cat-{node.TechCategory}";
        }

        private void HandleNodeClick(Node node)
        {
            if (node.Type != "folder") return;

            if (currentlyExpandedFolder?.Id == node.Id)
            {
                currentlyExpandedFolder = null;
                ScheduleConnectionRedraw();
                _deferFolderPopupPositionAfterRender = false;
            }
            else
            {
                currentlyExpandedFolder = node;
                _deferFolderPopupPositionAfterRender = true;
            }

            StateHasChanged();
        }

        private void CloseFolderPopup()
        {
            if (currentlyExpandedFolder != null)
            {
                currentlyExpandedFolder = null;
                _deferFolderPopupPositionAfterRender = false;
                ScheduleConnectionRedraw();
                StateHasChanged();
            }
        }
        private async Task ToggleNodeSelection(Node node)
        {
            if (node.Type != "vehicle") return;

            if (currentlyExpandedFolder != null)
            {
                var nodesInCurrentPopup = GetVisualNodesInFolderSequence(currentlyExpandedFolder)
                                            .Where(n => n.Type == "vehicle")
                                            .Select(n => n.Id)
                                            .ToHashSet();

                if (!nodesInCurrentPopup.Contains(node.Id))
                {
                    currentlyExpandedFolder = null;
                    _deferFolderPopupPositionAfterRender = false;
                }
            }

            var toggleResult = _selection.Toggle(node, maxRpLimit);

            if (toggleResult == SelectionToggleResult.LimitExceeded)
            {
                await NotifyLimitExceeded();
                return;
            }

            await CalculateAndUpdateTotals();
            UpdateUrlState();
            ScheduleConnectionRedraw();
            StateHasChanged();
        }

        // Уведомление о превышении лимита:
        private async Task NotifyLimitExceeded()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("techTreeFunctions.showRpLimitWarning",
                    Localization.Text(UIStrings.RPLimitExceededMessage));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to show limit warning: {ex.Message}");
            }
        }
        private bool IsFolderSelected(Node folderNode)
        {
            var children = GetVisualNodesInFolderSequence(folderNode);
            return children.Any(child => child.Type == "vehicle" && _selection.SelectedIds.Contains(child.Id));
        }
        private IReadOnlyList<Node> GetVisualNodesInFolderSequence(Node folderNode)
            => _layout.GetFolderChildren(folderNode);

        private async Task OnRpValueChanged(Node node, ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newValue))
            {
                _selection.SetRpOverride(node, newValue);

                await CalculateAndUpdateTotals();
                UpdateUrlState(); // Обновляем URL после изменения RP
                ScheduleConnectionRedraw();
                StateHasChanged();
            }
        }
        private async Task DrawConnectionLines()
        {
            if (JSRuntime == null || Dependencies == null || !Dependencies.Any()) return;
            try
            {
                var connectionData = Dependencies.Select(dep => new
                {
                    lineId = $"dep-{dep.NodeId}-{dep.PrerequisiteNodeId}",
                    sourceElementId = $"node-{dep.PrerequisiteNodeId}",
                    targetElementId = $"node-{dep.NodeId}"
                }).ToList();
                await JSRuntime.InvokeVoidAsync("techTreeFunctions.drawConnections", connectionData);
            }
            catch
            {
            }
        }

        // Публичные методы для скриншотов
        public async Task DownloadScreenshot()
        {
            try
            {
                var selectedRanks = GetSelectedRanks();
                if (selectedRanks.Count == 0)
                {
                    return;
                }

                var selectedNodes = GetSelectedNodes();
                var includePremium = UiSettings.Current.IncludePremiumInScreenshot
                                     && selectedNodes.Any(n => n.TechCategory != "standard");

                var filename = await JSRuntime.InvokeAsync<string>("techTreeFunctions.generateScreenshotFilename");

                var messages = new
                {
                    preparing = Localization.Text(UIStrings.PreparingScreenshot),
                    rendering = Localization.Text(UIStrings.CreatingScreenshot),
                    processing = Localization.Text(UIStrings.ProcessingScreenshot),
                    copying = Localization.Text(UIStrings.CopyingScreenshot),
                    clipboardSuccess = Localization.Text(UIStrings.ScreenshotCopied),
                    downloadProgress = Localization.Text(UIStrings.PreparingDownload),
                    downloadFallback = Localization.Text(UIStrings.ScreenshotSaved),
                    error = Localization.Text(UIStrings.ScreenshotError)
                };

                using var callbackRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("techTreeFunctions.downloadScreenshot",
                    selectedRanks.ToArray(),
                    filename,
                    UiSettings.Current.ShowSummaryOnScreenshot,
                    includePremium,
                    messages,
                    callbackRef,
                    callbackRef);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading screenshot: {ex.Message}");
            }
        }

        private List<Node> GetSelectedNodes() => _selection.SelectedNodes.ToList();

        private List<int> GetSelectedRanks() => _selection.SelectedRanks.ToList();

        [JSInvokable]
        public Task RedrawConnectionsDebouncedAsync()
        {
            return DrawConnectionLines();
        }

        [JSInvokable]
        public Task OnScreenshotProgress(string message)
        {
            return ScreenshotProgress.InvokeAsync(message);
        }

        [JSInvokable]
        public Task OnScreenshotComplete(bool success, string message)
        {
            return ScreenshotCompleted.InvokeAsync(new ScreenshotResult(success, message));
        }

        public async ValueTask DisposeAsync()
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            await TeardownTreeLayoutInteropAsync();
        }
    }
}
