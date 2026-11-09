using WTExpCalc.Models;

namespace WTExpCalc.Services.UrlState
{
    public interface IUrlStateManager
    {
        /// <summary>
        /// Обновляет URL с текущим состоянием выбранных юнитов.
        /// </summary>
        void UpdateUrlWithSelection(
            List<Node> allNodes,
            IReadOnlySet<int> selectedIds,
            IReadOnlyDictionary<int, int> originalRpValues,
            IReadOnlyDictionary<int, int> rpOverrides,
            bool includePremiumInShareUrl);

        /// <summary>
        /// Восстанавливает состояние из URL параметров.
        /// </summary>
        UrlSelectionState RestoreSelectionFromUrl(List<Node> allNodes, string currentUrl);

        /// <summary>
        /// Копирует текущий URL в буфер обмена.
        /// </summary>
        Task CopyCurrentUrlToClipboardAsync();
    }

    public class UrlSelectionState
    {
        public HashSet<int> SelectedNodeIds { get; set; } = new();
        public Dictionary<int, int> ModifiedRpValues { get; set; } = new();
    }
}
