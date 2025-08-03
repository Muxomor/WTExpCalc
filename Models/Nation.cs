using System.Text.Json.Serialization;
using WTExpCalc.Shared;

namespace WTExpCalc.Models
{
    public class Nation
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // ============ ДОБАВЛЕНО: Поле для английского названия (ОПЦИОНАЛЬНО) ============
        [JsonPropertyName("name_eng")]
        public string? NameEnglish { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// URL-friendly slug (английское название нации)
        /// </summary>
        [JsonIgnore]
        public string Slug => Name.ToLowerInvariant();

        /// <summary>
        /// Отображаемое название (русское, через FormattingUtils)
        /// </summary>
        [JsonIgnore]
        public string DisplayName => FormattingUtils.FormatNationName(Name, Id);
    }
}