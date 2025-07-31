using System.Collections.Generic;
using System.Linq;

namespace WTExpCalc.Shared
{
    public static class FormattingUtils
    {
        private static readonly Dictionary<string, string> NationDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

        /// <summary>
        /// Форматирует название нации для отображения
        /// </summary>
        /// <param name="nameFromDb">Английское название из БД</param>
        /// <param name="nationIdForFallback">ID нации для fallback отображения</param>
        /// <returns>Русское название нации</returns>
        public static string FormatNationName(string? nameFromDb, int? nationIdForFallback = null)
        {
            if (string.IsNullOrEmpty(nameFromDb))
            {
                return nationIdForFallback.HasValue ? $"ID: {nationIdForFallback.Value}" : "Неизвестная нация";
            }

            if (NationDisplayNames.TryGetValue(nameFromDb, out string? displayName))
            {
                return displayName;
            }

            // Fallback: капитализируем первую букву
            if (nameFromDb.Length == 1)
            {
                return nameFromDb.ToUpperInvariant();
            }
            return char.ToUpperInvariant(nameFromDb[0]) + nameFromDb.Substring(1);
        }

        /// <summary>
        /// Получает английский slug нации по русскому названию (обратный маппинг)
        /// </summary>
        /// <param name="russianName">Русское название нации</param>
        /// <returns>Английский slug или null если не найден</returns>
        public static string? GetNationSlugByRussianName(string? russianName)
        {
            if (string.IsNullOrEmpty(russianName))
                return null;

            var pair = NationDisplayNames.FirstOrDefault(kvp =>
                kvp.Value.Equals(russianName, StringComparison.OrdinalIgnoreCase));

            return pair.Key;
        }

        /// <summary>
        /// Проверяет, является ли slug валидным для нации
        /// </summary>
        /// <param name="slug">Slug для проверки</param>
        /// <returns>True если slug валиден</returns>
        public static bool IsValidNationSlug(string? slug)
        {
            return !string.IsNullOrEmpty(slug) && NationDisplayNames.ContainsKey(slug);
        }

        /// <summary>
        /// Получает все доступные slug наций
        /// </summary>
        /// <returns>Список всех валидных slug наций</returns>
        public static IEnumerable<string> GetAllValidNationSlugs()
        {
            return NationDisplayNames.Keys;
        }
    }
}