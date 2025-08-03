// Создать файл: Shared/SimpleGameLocalization.cs

using System.Collections.Generic;
using System;

namespace WTExpCalc.Shared
{
    /// <summary>
    /// Простая локализация игровых данных
    /// </summary>
    public static class SimpleGameLocalization
    {
        // Названия наций на английском
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

        // Типы техники на английском
        private static readonly Dictionary<string, string> VehicleTypesEn = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Авиация", "Aviation" },
            { "Вертолёты", "Helicopters" },
            { "Наземная техника", "Ground Forces" },
            { "Большой флот", "Fleet" },
            { "Малый флот", "Coastal Fleet" }
        };

        /// <summary>
        /// Получает локализованное название нации
        /// </summary>
        /// <param name="englishName">Английское название из БД</param>
        /// <param name="isEnglish">Нужен ли английский перевод</param>
        /// <param name="fallbackId">ID для fallback отображения</param>
        /// <returns>Локализованное название</returns>
        public static string GetNationName(string englishName, bool isEnglish, int? fallbackId = null)
        {
            if (isEnglish)
            {
                return NationNamesEn.TryGetValue(englishName, out var enName)
                    ? enName
                    : FirstLetterUpper(englishName);
            }
            else
            {
                // Используем существующий FormattingUtils для русского
                return FormattingUtils.FormatNationName(englishName, fallbackId);
            }
        }

        /// <summary>
        /// Получает локализованное название типа техники
        /// </summary>
        /// <param name="russianName">Русское название</param>
        /// <param name="englishName">Английское название из БД (может быть null)</param>
        /// <param name="isEnglish">Нужен ли английский перевод</param>
        /// <returns>Локализованное название</returns>
        public static string GetVehicleTypeName(string russianName, string? englishName, bool isEnglish)
        {
            if (isEnglish)
            {
                // Приоритет: name_eng из БД, затем наш словарь, затем русское название
                return !string.IsNullOrEmpty(englishName)
                    ? englishName
                    : (VehicleTypesEn.TryGetValue(russianName, out var enName)
                        ? enName
                        : russianName);
            }
            else
            {
                return russianName;
            }
        }

        /// <summary>
        /// Получает локализованное название узла техники
        /// </summary>
        /// <param name="russianName">Русское название</param>
        /// <param name="englishName">Английское название из БД (может быть null)</param>
        /// <param name="isEnglish">Нужен ли английский перевод</param>
        /// <returns>Локализованное название</returns>
        public static string GetNodeName(string russianName, string? englishName, bool isEnglish)
        {
            return isEnglish && !string.IsNullOrEmpty(englishName)
                ? englishName
                : russianName;
        }

        /// <summary>
        /// Делает первую букву заглавной
        /// </summary>
        private static string FirstLetterUpper(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Length == 1
                ? input.ToUpperInvariant()
                : char.ToUpperInvariant(input[0]) + input.Substring(1);
        }
    }
}