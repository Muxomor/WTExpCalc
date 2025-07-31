using Microsoft.AspNetCore.Components;
using System.Web;
using System.Text.Json;
using WTExpCalc.Models;
using Microsoft.JSInterop;

namespace WTExpCalc.Services
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

        public void UpdateUrlWithSelection(List<Node> allNodes, HashSet<int> selectedIds, Dictionary<int, int?> originalRpValues)
        {
            try
            {
                var selectedNodes = allNodes.Where(n => selectedIds.Contains(n.Id) && n.RequiredExp.HasValue).ToList();

                if (!selectedNodes.Any())
                {
                    // Если ничего не выбрано, убираем параметры из URL
                    var cleanUri = GetBaseUri();
                    _navigationManager.NavigateTo(cleanUri, replace: true);
                    return;
                }

                var urlData = new List<string>();
                var rpData = new List<string>();

                foreach (var node in selectedNodes)
                {
                    urlData.Add(node.ExternalId);

                    // Проверяем, изменено ли значение RP
                    var currentRp = node.RequiredExp!.Value;
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

                if (rpData.Any())
                {
                    queryParams.Add($"rp={string.Join(",", rpData)}");
                }

                var baseUri = GetBaseUri();
                var newUrl = $"{baseUri}?{string.Join("&", queryParams)}";

                _navigationManager.NavigateTo(newUrl, replace: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating URL with selection: {ex.Message}");
            }
        }

        public async Task<UrlSelectionState> RestoreSelectionFromUrlAsync(List<Node> allNodes, string currentUrl)
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
                            int.TryParse(parts[1].Trim(), out var rpValue))
                        {
                            result.ModifiedRpValues[node.Id] = rpValue;
                        }
                    }
                }

                Console.WriteLine($"Restored {result.SelectedNodeIds.Count} selected nodes and {result.ModifiedRpValues.Count} RP modifications from URL");
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
                Console.WriteLine("Current URL copied to clipboard");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error copying URL to clipboard: {ex.Message}");
            }
        }

        private string GetBaseUri()
        {
            var uri = new Uri(_navigationManager.Uri);
            return $"{uri.Scheme}://{uri.Host}{(uri.Port != 80 && uri.Port != 443 ? $":{uri.Port}" : "")}{uri.AbsolutePath}";
        }
    }
}