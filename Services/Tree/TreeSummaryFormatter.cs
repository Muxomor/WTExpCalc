using WTExpCalc.Models;
using WTExpCalc.Services.Localization;
using WTExpCalc.Shared.Localization;

namespace WTExpCalc.Services.Tree
{
    /// <summary>
    /// Формирует текст выбранной техники для копирования в буфер обмена.
    /// </summary>
    public sealed class TreeSummaryFormatter
    {
        private readonly ILocalizationService _localization;

        public TreeSummaryFormatter(ILocalizationService localization)
        {
            _localization = localization;
        }

        public string Format(
            IReadOnlyList<Node> selectedNodes,
            IReadOnlyDictionary<int, int> originalRpValues,
            IReadOnlyDictionary<int, int> rpOverrides,
            long totalRp)
        {
            if (selectedNodes.Count == 0)
            {
                return string.Empty;
            }

            // Разделяем стандартную и премиумную технику
            var standardNodes = selectedNodes.Where(n => n.TechCategory == "standard").ToList();
            var premiumNodes = selectedNodes.Where(n => n.TechCategory != "standard").ToList();

            var result = new List<string>();

            // Обрабатываем стандартную технику (группируем по рангам)
            if (standardNodes.Count > 0)
            {
                var groupedByRank = standardNodes
                    .GroupBy(n => n.Rank)
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var rankGroup in groupedByRank)
                {
                    var rankNumber = rankGroup.Key;
                    var vehicleNames = rankGroup
                        .OrderBy(n => n.ColumnIndex ?? 0)
                        .Select(vehicle => FormatVehicleName(vehicle, originalRpValues, rpOverrides))
                        .ToList();

                    // Формируем строку для ранга с учетом языка
                    string rankLine = $"{_localization.Text(UIStrings.Rank)} {ToRomanNumeral(rankNumber)}:\n{string.Join(", ", vehicleNames)}";
                    result.Add(rankLine);
                }
            }

            // Добавляем премиумную технику простым списком через запятую
            if (premiumNodes.Count > 0)
            {
                var premiumNames = premiumNodes
                    .OrderBy(n => n.Rank)
                    .ThenBy(n => n.ColumnIndex ?? 0)
                    // Для премиумной техники берем ТОЛЬКО локализованное имя, без RP
                    .Select(vehicle => _localization.NodeName(vehicle.Name, vehicle.NameEnglish))
                    .ToList();

                result.Add(string.Join(", ", premiumNames));
            }

            // Добавляем общее количество опыта в конец
            string totalRpText = $"{_localization.Text(UIStrings.TotalRP)}: {totalRp:N0}";
            return string.Join("\n\n", result) + (result.Count > 0 ? "\n\n" + totalRpText : totalRpText);
        }

        public static int GetEffectiveRp(Node node, IReadOnlyDictionary<int, int> rpOverrides)
        {
            return rpOverrides.TryGetValue(node.Id, out var value)
                ? value
                : node.RequiredExp ?? 0;
        }

        public static bool IsRpValueModified(
            Node node,
            IReadOnlyDictionary<int, int> originalRpValues,
            IReadOnlyDictionary<int, int> rpOverrides)
        {
            if (!node.RequiredExp.HasValue)
            {
                return false;
            }

            return rpOverrides.TryGetValue(node.Id, out var currentValue)
                   && originalRpValues.TryGetValue(node.Id, out var originalValue)
                   && currentValue != originalValue;
        }

        private string FormatVehicleName(
            Node vehicle,
            IReadOnlyDictionary<int, int> originalRpValues,
            IReadOnlyDictionary<int, int> rpOverrides)
        {
            string vehicleName = _localization.NodeName(vehicle.Name, vehicle.NameEnglish);

            if (!IsRpValueModified(vehicle, originalRpValues, rpOverrides))
            {
                return vehicleName;
            }

            var currentRp = GetEffectiveRp(vehicle, rpOverrides);
            var originalRp = originalRpValues.TryGetValue(vehicle.Id, out var original) ? original : 0;
            return $"{vehicleName} ({currentRp} / {originalRp})";
        }

        private static string ToRomanNumeral(int number)
        {
            return number switch
            {
                1 => "I",
                2 => "II",
                3 => "III",
                4 => "IV",
                5 => "V",
                6 => "VI",
                7 => "VII",
                8 => "VIII",
                _ => number.ToString()
            };
        }
    }
}
