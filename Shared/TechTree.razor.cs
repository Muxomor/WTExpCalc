using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using WTExpCalc.Models;
using WTExpCalc.Services;

namespace WTExpCalc.Shared
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
        private List<Node> GridNodes = new();
        private List<NodeDependency> Dependencies = new();
        private Node? currentlyExpandedFolder;
        private TreeLoadState LoadingState = TreeLoadState.Loading;
        private string? _loadErrorMessage;
        private List<RankRequirement> rankRequirements = new();
        private HashSet<int> selectedNodeIds = new();
        private int MaxRank = 0;
        private string GridStyles = "";
        private readonly Dictionary<int, int> MaxRowIndexPerRank = new();
        private int TotalDataGridRows = 0;
        private long totalSelectedRp = 0;
        private long totalSelectedSl = 0;

        private string LoadingStateCss => LoadingState switch
        {
            TreeLoadState.Loading => "loading",
            TreeLoadState.Error => "error",
            _ => "loaded"
        };

        // Словарь для хранения исходных значений RP
        private Dictionary<int, int?> originalRpValues = new();
        // Флаг для предотвращения циклических обновлений URL
        private bool _suppressUrlUpdate = false;

        private int _previousNationId = -1;
        private int _previousTypeId = -1;
        private long? _previousRpLimit;
        private bool _isFirstLoad = true;

        private const int NodeCardWidth = 204;
        private const int NodeCardHeight = 87;

        private long? maxRpLimit = null;

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
                            $"node-{currentlyExpandedFolder.Id}",
                            "tech-tree-container-id");
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
            if (limit.HasValue && totalSelectedRp > limit.Value)
            {
                removedSelection = RemoveExcessSelection(limit.Value);
            }

            await CalculateAndUpdateTotals();

            if (removedSelection)
            {
                UpdateUrlState();
            }

            ScheduleConnectionRedraw();
            StateHasChanged();
        }

        // Метод для снятия выбора с последних узлов до достижения лимита:
        private bool RemoveExcessSelection(long limit)
        {
            var selectedNodesList = selectedNodeIds
                .Select(id => NodeLookup.TryGetValue(id, out var node) ? node : null)
                .OfType<Node>()
                .Where(node => node.RequiredExp.HasValue)
                .OrderByDescending(node => node.Rank) // Снимаем с самых высоких рангов
                .ThenByDescending(node => node.ColumnIndex ?? 0)
                .ToList();

            long currentTotal = totalSelectedRp;
            var removed = false;

            foreach (var node in selectedNodesList)
            {
                if (currentTotal <= limit) break;

                long nodeRp = node.RequiredExp ?? 0;
                selectedNodeIds.Remove(node.Id);
                currentTotal -= nodeRp;
                removed = true;
            }

            return removed;
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

                MaxRowIndexPerRank.Clear();
                GridNodes.Clear();
                Dependencies.Clear();
                rankRequirements.Clear();
                selectedNodeIds.Clear();
                originalRpValues.Clear();
                currentlyExpandedFolder = null;
                AllNodesFlat = new();
                NodeLookup = new();
                await ResetTotalsAsync();
                StateHasChanged();

                AllNodesFlat = await Api.GetAllNodesFlatAsync(NationId, TypeId);
                if (AllNodesFlat == null || AllNodesFlat.Count == 0)
                {
                    MaxRank = 0;
                    GridStyles = "";
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
                PrepareGridNodesAndCalculateRows();
                Dependencies = await Api.GetAllDependenciesAsync(NodeLookup.Keys.ToList());
                CalculateGridDimensions();

                // Восстанавливаем состояние из URL
                RestoreStateFromUrl();

                LoadingState = TreeLoadState.Loaded;
                _suppressUrlUpdate = false; // Разрешаем обновление URL

                await CalculateAndUpdateTotals();

                if (maxRpLimit.HasValue && totalSelectedRp > maxRpLimit.Value)
                {
                    RemoveExcessSelection(maxRpLimit.Value);
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
            totalSelectedRp = 0;
            totalSelectedSl = 0;

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

                selectedNodeIds = urlState.SelectedNodeIds;

                // Восстанавливаем измененные значения RP
                foreach (var kvp in urlState.ModifiedRpValues)
                {
                    if (NodeLookup.TryGetValue(kvp.Key, out var node) && node.RequiredExp.HasValue)
                    {
                        node.RequiredExp = kvp.Value;
                    }
                }
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
                    selectedNodeIds,
                    originalRpValues,
                    UiSettings.Current.IncludePremiumInShareUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating URL state: {ex.Message}");
            }
        }
        private async Task CalculateAndUpdateTotals()
        {
            totalSelectedRp = 0;
            totalSelectedSl = 0;

            var allSelectedNodes = selectedNodeIds
                .Select(id => NodeLookup.TryGetValue(id, out var node) ? node : null)
                .OfType<Node>()
                .Where(node => node.Type == "vehicle")
                .ToList();

            // Подсчитываем опыт и SL только для узлов с опытом
            foreach (var node in allSelectedNodes.Where(node => node.RequiredExp.HasValue))
            {
                totalSelectedRp += node.RequiredExp ?? 0;
                totalSelectedSl += node.SilverCost ?? 0;
            }

            // Для отображения имен используем ВСЕ выбранные узлы
            var namesString = string.Join(", ", allSelectedNodes.Select(GetNodeDisplayName));
            var formattedText = SummaryFormatter.Format(allSelectedNodes, originalRpValues, totalSelectedRp);

            if (OnTotalsUpdated.HasDelegate)
            {
                await OnTotalsUpdated.InvokeAsync(new TotalsUpdateEventArgs
                    {
                        TotalRp = totalSelectedRp,
                        TotalSl = totalSelectedSl,
                        SelectedNames = namesString,
                        FormattedText = formattedText
                    });
            }

            // Уведомляем родительский компонент об изменении выбранных рангов
            if (OnSelectedRanksChanged.HasDelegate)
            {
                var selectedRanks = GetSelectedRanks();
                await OnSelectedRanksChanged.InvokeAsync(selectedRanks);
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
                    totalSelectedRp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update nodes limit state: {ex.Message}");
            }
        }
        private void PrepareGridNodesAndCalculateRows()
        {
            GridNodes.Clear();
            MaxRowIndexPerRank.Clear();
            TotalDataGridRows = 0;

            if (AllNodesFlat == null || !AllNodesFlat.Any()) return;

            foreach (var node in AllNodesFlat)
            {
                int currentMax = MaxRowIndexPerRank.TryGetValue(node.Rank, out var max) ? max : -1;
                if ((node.RowIndex ?? 0) > currentMax) MaxRowIndexPerRank[node.Rank] = node.RowIndex ?? 0;
                else if (!MaxRowIndexPerRank.ContainsKey(node.Rank)) MaxRowIndexPerRank[node.Rank] = node.RowIndex ?? 0;
            }
            TotalDataGridRows = MaxRowIndexPerRank.Sum(kvp => kvp.Value + 1);

            foreach (var node in AllNodesFlat)
            {
                bool isGridNode = false;

                if (node.Type == "folder")
                {
                    isGridNode = true;
                }
                else if (node.Type == "vehicle")
                {
                    if (!node.OrderInFolder.HasValue)
                    {
                        isGridNode = true;
                    }
                }
                if (isGridNode)
                {
                    GridNodes.Add(node);
                }
            }
        }

        private void CalculateGridDimensions()
        {
            MaxRank = AllNodesFlat.Any() ? AllNodesFlat.Max(n => n.Rank) : 0;
            var standardNodes = AllNodesFlat.Where(n => n.TechCategory == "standard");
            int maxStd = standardNodes.Any() ? standardNodes.Max(n => n.ColumnIndex ?? 0) : -1;
            var premiumNodes = AllNodesFlat.Where(n => n.TechCategory != "standard");
            int maxPrem = premiumNodes.Any() ? premiumNodes.Max(n => n.ColumnIndex ?? 0) : -1;
            int stdCount = Math.Max(0, maxStd + 1);
            int premCount = Math.Max(0, maxPrem + 1);

            string gridRowsStyle = "auto";
            if (TotalDataGridRows > 0)
                gridRowsStyle += $" repeat({TotalDataGridRows}, {NodeCardHeight}px)";
            else if (MaxRank > 0)
                gridRowsStyle += $" repeat({MaxRank}, {NodeCardHeight}px)";

            GridStyles =
                $"display: grid; " +
                $"grid-template-rows: {gridRowsStyle}; " +
                $"grid-template-columns: 60px {(stdCount > 0 ? $"repeat({stdCount}, {NodeCardWidth}px) " : "")}{(premCount > 0 ? $"repeat({premCount}, {NodeCardWidth}px)" : "")}; " +
                $"gap: 30px 40px; position: relative;";
        }

        private List<int> GetActiveRanks()
        {
            // Получаем ранги из rankRequirements и из техники
            var ranksFromRequirements = rankRequirements.Select(r => r.PreviousRank).ToHashSet();
            var ranksFromNodes = AllNodesFlat.Select(n => n.Rank).ToHashSet();

            // Объединяем и сортируем
            var allActiveRanks = ranksFromRequirements.Union(ranksFromNodes).OrderBy(r => r).ToList();

            return allActiveRanks;
        }

        private string GetNodeGridStyle(Node node)
        {
            var activeRanks = GetActiveRanks();

            int col = node.TechCategory == "standard"
                ? 1 + (node.ColumnIndex ?? 0) + 1
                : 1 + ((AllNodesFlat.Where(n => n.TechCategory == "standard").Max(n => n.ColumnIndex ?? 0)) + 1) + (node.ColumnIndex ?? 0) + 1;

            // Вычисляем строку на базе активных рангов
            int calculatedRow = 1;
            foreach (var activeRank in activeRanks)
            {
                if (activeRank >= node.Rank) break;
                calculatedRow += (MaxRowIndexPerRank.TryGetValue(activeRank, out var prev) ? prev : 0) + 1;
            }
            calculatedRow += (node.RowIndex ?? 0);

            return $"grid-column: {col}; grid-row: {calculatedRow + 1}; z-index:1; position:relative;";
        }

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

            bool isSelected = selectedNodeIds.Contains(node.Id);

            if (isSelected)
            {
                // Снятие выбора всегда разрешено
                selectedNodeIds.Remove(node.Id);

                // Восстанавливаем оригинальное значение RP при снятии выбора
                if (originalRpValues.TryGetValue(node.Id, out var originalRp))
                {
                    node.RequiredExp = originalRp;
                }
            }
            else
            {
                long nodeRp = node.RequiredExp ?? 0;

                if (!maxRpLimit.HasValue)
                {
                    // Нет лимита, выбираем как обычно
                    selectedNodeIds.Add(node.Id);
                }
                else
                {
                    long remaining = maxRpLimit.Value - totalSelectedRp;

                    if (nodeRp <= remaining)
                    {
                        // Нода целиком помещается в лимит (в том числе бесплатная премиумная)
                        selectedNodeIds.Add(node.Id);
                    }
                    else if (remaining > 0 && nodeRp > 0)
                    {
                        // Автоподгонка: уменьшаем RP узла до остатка лимита
                        node.RequiredExp = (int)remaining;
                        selectedNodeIds.Add(node.Id);
                    }
                    else
                    {
                        // Лимит уже достигнут, показываем предупреждение
                        await NotifyLimitExceeded();
                        return;
                    }
                }
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
            return children.Any(child => child.Type == "vehicle" && selectedNodeIds.Contains(child.Id));
        }
        private RenderFragment RenderFolderNode(Node node) => builder =>
        {
            bool isCurrentlyExpanded = currentlyExpandedFolder?.Id == node.Id;
            bool containsSelected = IsFolderSelected(node);
            var children = GetVisualNodesInFolderSequence(node)
                        .Where(c => c.Type == "vehicle")
                        .ToList();
            int totalChildren = children.Count;
            int selectedChildrenCount = children.Count(c => selectedNodeIds.Contains(c.Id));
            string displayName = GetNodeDisplayName(node);

            int seq = 0;

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class",
                $"node-content folder-node {(isCurrentlyExpanded ? "folder-active" : "")} {(containsSelected ? "folder-contains-selected" : "")}");
            builder.AddAttribute(seq++, "id", $"node-{node.Id}");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => HandleNodeClick(node)));

            // Иконка
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "node-icon");
            builder.AddAttribute(seq++, "style", $"background-image:url('{node.ImageUrl}')");
            builder.CloseElement();

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "node-name");
            builder.AddAttribute(seq++, "title", displayName);
            builder.AddContent(seq++, displayName);
            builder.CloseElement();

            if (totalChildren > 0)
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "folder-child-counter");
                builder.AddContent(seq++, $"{selectedChildrenCount} / {totalChildren}");
                builder.CloseElement();

                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "folder-index-squares");
                builder.AddAttribute(seq++, "style", $"--folder-count: {children.Count}");
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    bool isChildSelected = selectedNodeIds.Contains(child.Id);

                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", isChildSelected ? "folder-index selected" : "folder-index");
                    builder.AddAttribute(seq++, "title", GetNodeDisplayName(child));
                    builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => ToggleNodeSelection(child)));
                    builder.AddEventStopPropagationAttribute(seq++, "onclick", true);
                    builder.AddContent(seq++, (i + 1).ToString());
                    builder.CloseElement();
                }
                builder.CloseElement();
            }

            // Плюс/минус
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "folder-toggle-icon");
            builder.AddContent(seq++, isCurrentlyExpanded ? "−" : "+");
            builder.CloseElement();

            builder.CloseElement();
        };

        private List<Node> GetVisualNodesInFolderSequence(Node folderNode)
        {
            return AllNodesFlat
                .Where(n =>
                    n.Rank == folderNode.Rank
                    && n.ColumnIndex == folderNode.ColumnIndex
                    && n.TechCategory == folderNode.TechCategory 
                    && n.RowIndex == folderNode.RowIndex
                    && n.OrderInFolder.HasValue)
                .OrderBy(n => n.OrderInFolder!.Value)
                .ToList();
        }

        private async Task OnRpValueChanged(Node node, ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out int newValue))
            {
                node.RequiredExp = newValue;
                await CalculateAndUpdateTotals();
                UpdateUrlState(); // Обновляем URL после изменения RP
                ScheduleConnectionRedraw();
                StateHasChanged();
            }
        }
        private RenderFragment RenderVehicleNode(Node node) => __builder =>
        {
            bool isSelected = selectedNodeIds.Contains(node.Id);
            bool isRpModified = TreeSummaryFormatter.IsRpValueModified(node, originalRpValues);
            string displayName = GetNodeDisplayName(node);
            int seq = 0;

            __builder.OpenElement(seq++, "div");
            __builder.AddAttribute(seq++, "id", $"node-{node.Id}");
            __builder.AddAttribute(seq++, "class",
                $"node-content vehicle-node {(node.RequiredExp.HasValue ? "rp-editable" : "")} {(isSelected ? "node-selected" : "")}");
            __builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => ToggleNodeSelection(node)));

            __builder.OpenElement(seq++, "div");
            __builder.AddAttribute(seq++, "class", "node-icon");
            __builder.AddAttribute(seq++, "style", $"background-image:url('{node.ImageUrl}')");
            __builder.CloseElement();

            __builder.OpenElement(seq++, "div");
            __builder.AddAttribute(seq++, "class", "node-info");

            __builder.OpenElement(seq++, "div");
            __builder.AddAttribute(seq++, "class", "node-name");
            __builder.AddAttribute(seq++, "title", displayName);
            __builder.AddContent(seq++, displayName);
            __builder.CloseElement();

            __builder.OpenElement(seq++, "div");
            __builder.AddAttribute(seq++, "class", "node-details");

            if (node.BattleRating.HasValue)
            {
                __builder.OpenElement(seq++, "div");
                __builder.AddAttribute(seq++, "class", "node-detail-item node-br");
                // ИСПРАВЛЕНО: используем локализованную метку БР
                __builder.AddContent(seq++, $"{Localization.Text(UIStrings.BR)}: {node.BattleRating}");
                __builder.CloseElement();
            }

            __builder.OpenElement(seq++, "div");
            __builder.AddAttribute(seq++, "class", "node-detail-item node-exp");
            if (node.RequiredExp.HasValue)
            {
                __builder.AddContent(seq++, $"{Localization.Text(UIStrings.RP)}: ");
                __builder.OpenElement(seq++, "input");
                __builder.AddAttribute(seq++, "type", "number");
                __builder.AddAttribute(seq++, "class", $"rp-input {(isRpModified ? "rp-modified" : "")}");
                __builder.AddAttribute(seq++, "min", "0");
                __builder.AddAttribute(seq++, "value", BindConverter.FormatValue(node.RequiredExp));
                __builder.AddAttribute(seq++, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this,
                    e => OnRpValueChanged(node, e)));
                __builder.AddAttribute(seq++, "onclick", "event.stopPropagation();");
                __builder.AddElementReferenceCapture(seq++, capturedRef => { });
                __builder.CloseElement();
            }
            __builder.CloseElement();

            __builder.CloseElement();
            __builder.CloseElement();
            __builder.CloseElement();
        };

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

        private List<Node> GetSelectedNodes()
        {
            return selectedNodeIds
                .Select(id => NodeLookup.TryGetValue(id, out var node) ? node : null)
                .OfType<Node>()
                .Where(node => node.Type == "vehicle")
                .ToList();
        }

        private List<int> GetSelectedRanks()
        {
            return GetSelectedNodes()
                .Select(node => node.Rank)
                .Distinct()
                .OrderBy(rank => rank)
                .ToList();
        }

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
