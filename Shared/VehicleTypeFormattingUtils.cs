using System.Collections.Generic;
using System.Linq;

namespace WTExpCalc.Shared
{
    public static class VehicleTypeFormattingUtils
    {
        // Маппинг русских названий на английские slug
        private static readonly Dictionary<string, string> VehicleTypeSlugMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Авиация", "aircraft" },
            { "Вертолёты", "helicopters" },
            { "Наземная техника", "tanks" },
            { "Большой флот", "naval" },
            { "Малый флот", "coastal" }
        };

        // Обратный маппинг для восстановления по slug
        private static readonly Dictionary<string, string> SlugToDisplayNameMap = VehicleTypeSlugMap
            .ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Преобразует русское название типа техники в URL-slug
        /// </summary>
        /// <param name="russianName">Русское название из БД</param>
        /// <returns>URL-friendly slug</returns>
        public static string GetSlugFromRussianName(string? russianName)
        {
            if (string.IsNullOrEmpty(russianName))
                return "unknown";

            if (VehicleTypeSlugMap.TryGetValue(russianName, out string? slug))
                return slug;

            // Fallback: создаем slug из русского названия
            return russianName
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("ё", "e")
                .Replace("ъ", "")
                .Replace("ь", "");
        }

        /// <summary>
        /// Получает русское отображаемое название по slug
        /// </summary>
        /// <param name="slug">URL slug</param>
        /// <returns>Русское название для отображения</returns>
        public static string? GetDisplayNameFromSlug(string? slug)
        {
            if (string.IsNullOrEmpty(slug))
                return null;

            if (SlugToDisplayNameMap.TryGetValue(slug, out string? displayName))
                return displayName;

            return null; // Если slug не найден
        }

        /// <summary>
        /// Проверяет, является ли slug валидным
        /// </summary>
        /// <param name="slug">URL slug для проверки</param>
        /// <returns>True если slug валиден</returns>
        public static bool IsValidSlug(string? slug)
        {
            return !string.IsNullOrEmpty(slug) && SlugToDisplayNameMap.ContainsKey(slug);
        }

        /// <summary>
        /// Получает все доступные slug
        /// </summary>
        /// <returns>Список всех валидных slug</returns>
        public static IEnumerable<string> GetAllValidSlugs()
        {
            return SlugToDisplayNameMap.Keys;
        }

        /// <summary>
        /// Получает маппинг slug -> отображаемое название для всех типов
        /// </summary>
        /// <returns>Словарь slug -> русское название</returns>
        public static Dictionary<string, string> GetAllSlugDisplayPairs()
        {
            return new Dictionary<string, string>(SlugToDisplayNameMap);
        }
    }
}