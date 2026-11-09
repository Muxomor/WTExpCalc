using Microsoft.AspNetCore.Components;
using System.Web;
using Microsoft.JSInterop;
using WTExpCalc.Models;

namespace WTExpCalc.Services.UrlState
{
    public class UrlStateManager : IUrlStateManager
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;

        public UrlStateManager(NavigationManager navigationManager, IJSRuntime jsRuntime)
        {
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
        }

        public void UpdateUrlWithSelection(
            List<Node> allNodes,
            IReadOnlySet<int> selectedIds,
            IReadOnlyDictionary<int, int> originalRpValues,
            IReadOnlyDictionary<int, int> rpOverrides,
            bool includePremiumInShareUrl)
        {
            try
            {
                var selectedNodes = allNodes
                    .Where(n => selectedIds.Contains(n.Id)
                                && n.Type == "vehicle"
                                && (n.RequiredExp.HasValue
                                    || n.TechCategory == "standard"
                                    || includePremiumInShareUrl))
                    .ToList();

                if (selectedNodes.Count == 0)
                {
                    // Если ничего не выбрано, убираем параметры из URL
                    _navigationManager.NavigateTo(GetBaseUri(), replace: true);
                    return;
                }

                var urlData = new List<string>();
                var rpData = new List<string>();

                foreach (var node in selectedNodes)
                {
                    urlData.Add(node.ExternalId);

                    if (!node.RequiredExp.HasValue)
                    {
                        continue;
                    }

                    // Проверяем, изменено ли значение RP
                    var currentRp = rpOverrides.TryGetValue(node.Id, out var overrideRp)
                        ? overrideRp
                        : node.RequiredExp.Value;

                    var originalRp = originalRpValues.TryGetValue(node.Id, out var orig) ? orig : currentRp;

                    if (currentRp != originalRp)
                    {
                        rpData.Add($"{node.ExternalId}:{currentRp}");
                    }
                }

                // Формируем URL параметры
                var queryParams = new List<string>
                {
                    $"selected={string.Join(",", urlData)}"
                };

                if (rpData.Count > 0)
                {
                    queryParams.Add($"rp={string.Join(",", rpData)}");
                }

                var newUrl = $"{GetBaseUri()}?{string.Join("&", queryParams)}";
                _navigationManager.NavigateTo(newUrl, replace: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating URL with selection: {ex.Message}");
            }
        }

        public UrlSelectionState RestoreSelectionFromUrl(List<Node> allNodes, string currentUrl)
        {
            var result = new UrlSelectionState();

            try
            {
                var uri = new Uri(currentUrl);
                var query = HttpUtility.ParseQueryString(uri.Query);

                var selectedParam = query["selected"];
                var rpParam = query["rp"];

                if (string.IsNullOrEmpty(selectedParam))
                {
                    return result;
                }

                // Создаем словарь для быстрого поиска по external_id
                var nodesByExternalId = allNodes
                    .Where(n => !string.IsNullOrEmpty(n.ExternalId))
                    .ToDictionary(n => n.ExternalId, n => n);

                // Восстанавливаем выбранные юниты
                var selectedExternalIds = selectedParam.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var externalId in selectedExternalIds)
                {
                    if (nodesByExternalId.TryGetValue(externalId.Trim(), out var node))
                    {
                        result.SelectedNodeIds.Add(node.Id);
                    }
                }

                // Восстанавливаем измененные значения RP
                if (!string.IsNullOrEmpty(rpParam))
                {
                    var rpPairs = rpParam.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var rpPair in rpPairs)
                    {
                        var parts = rpPair.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2 &&
                            nodesByExternalId.TryGetValue(parts[0].Trim(), out var node) &&
                            node.RequiredExp.HasValue &&
                            int.TryParse(parts[1].Trim(), out var rpValue))
                        {
                            result.ModifiedRpValues[node.Id] = rpValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring selection from URL: {ex.Message}");
            }

            return result;
        }

        public async Task CopyCurrentUrlToClipboardAsync()
        {
            try
            {
                var currentUrl = _navigationManager.Uri;
                await _jsRuntime.InvokeVoidAsync("techTreeFunctions.copyTextToClipboard_fallback", currentUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying URL to clipboard: {ex.Message}");
            }
        }

        private string GetBaseUri()
        {
            var uri = new Uri(_navigationManager.Uri);
            return uri.GetLeftPart(UriPartial.Path);
        }
    }
}
