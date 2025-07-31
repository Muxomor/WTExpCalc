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