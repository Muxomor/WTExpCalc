using System.Collections.Generic;
using System.Linq;

namespace WTExpCalc.Shared
{
    public static class GameDataLocalization
    {
        private static readonly Dictionary<string, string> NationNamesRu = new(StringComparer.OrdinalIgnoreCase)
        {
            { "usa", "США" },
            { "germany", "Германия" },
            { "ussr", "СССР" },
            { "britain", "Великобритания" },
            { "japan", "Япония" },
            { "china", "Китай" },
            { "italy", "Италия" },
            { "france", "Франция" },
            { "sweden", "Швеция" },
            { "israel", "Израиль" }
        };

        private static readonly Dictionary<string, string> NationNamesEn = new(StringComparer.OrdinalIgnoreCase)
        {
            { "usa", "USA" },
            { "germany", "Germany" },
            { "ussr", "USSR" },
            { "britain", "Britain" },
            { "japan", "Japan" },
            { "china", "China" },
            { "italy", "Italy" },
            { "france", "France" },
            { "sweden", "Sweden" },
            { "israel", "Israel" }
        };

        private static readonly Dictionary<string, string> VehicleTypeNamesEn = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Авиация", "Aviation" },
            { "Вертолёты", "Helicopters" },
            { "Наземная техника", "Ground Forces" },
            { "Большой флот", "Fleet" },
            { "Малый флот", "Coastal Fleet" }
        };

        private static readonly Dictionary<string, string> VehicleTypeSlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Авиация", "aircraft" },
            { "Вертолёты", "helicopters" },
            { "Наземная техника", "tanks" },
            { "Большой флот", "naval" },
            { "Малый флот", "coastal" }
        };

        public static string NationName(string name, bool isEnglish, int? id = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return id.HasValue
                    ? $"ID: {id.Value}"
                    : isEnglish ? "Unknown nation" : "Неизвестная нация";
            }

            var map = isEnglish ? NationNamesEn : NationNamesRu;
            return map.TryGetValue(name, out var localized) ? localized : FirstLetterUpper(name);
        }

        public static string VehicleTypeName(string russianName, string? englishName, bool isEnglish)
        {
            if (isEnglish)
            {
                return !string.IsNullOrEmpty(englishName)
                    ? englishName
                    : VehicleTypeNamesEn.TryGetValue(russianName, out var en) ? en : russianName;
            }

            return russianName;
        }

        public static string NodeName(string russianName, string? englishName, bool isEnglish)
            => isEnglish && !string.IsNullOrEmpty(englishName) ? englishName : russianName;

        public static string VehicleTypeSlug(string? russianName)
        {
            if (string.IsNullOrEmpty(russianName))
                return "unknown";

            if (VehicleTypeSlugs.TryGetValue(russianName, out var slug))
                return slug;

            return russianName
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("ё", "e")
                .Replace("ъ", "")
                .Replace("ь", "");
        }

        public static bool IsValidVehicleTypeSlug(string? slug)
            => !string.IsNullOrEmpty(slug) && VehicleTypeSlugs.Values.Contains(slug, StringComparer.OrdinalIgnoreCase);

        private static string FirstLetterUpper(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Length == 1
                ? input.ToUpperInvariant()
                : char.ToUpperInvariant(input[0]) + input.Substring(1);
        }
    }
}
