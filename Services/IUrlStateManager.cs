using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public interface IUrlStateManager
    {
        /// <summary>
        /// Обновляет URL с текущим состоянием выбранных юнитов
        /// </summary>
        void UpdateUrlWithSelection(List<Node> allNodes, HashSet<int> selectedIds, Dictionary<int, int?> originalRpValues);

        /// <summary>
        /// Восстанавливает состояние из URL параметров
        /// </summary>
        Task<UrlSelectionState> RestoreSelectionFromUrlAsync(List<Node> allNodes, string currentUrl);

        /// <summary>
        /// Копирует текущий URL в буфер обмена
        /// </summary>
        Task CopyCurrentUrlToClipboardAsync();
    }

    public class UrlSelectionState
    {
        public HashSet<int> SelectedNodeIds { get; set; } = new();
        public Dictionary<int, int> ModifiedRpValues { get; set; } = new();
    }

    public class ScreenshotRankRange
    {
        public int MinRank { get; set; }
        public int MaxRank { get; set; }
        public bool IsValid => MinRank > 0 && MaxRank > 0 && MinRank <= MaxRank;
    }
}