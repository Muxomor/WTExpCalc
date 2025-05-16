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

            if (nameFromDb.Length == 1)
            {
                return nameFromDb.ToUpperInvariant();
            }
            return char.ToUpperInvariant(nameFromDb[0]) + nameFromDb.Substring(1);
        }
    }
}