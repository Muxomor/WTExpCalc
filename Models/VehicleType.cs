using System.Text.Json.Serialization;
using WTExpCalc.Shared;

namespace WTExpCalc.Models
{
    public class VehicleType
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// URL-friendly slug, вычисляется автоматически из русского названия
        /// </summary>
        [JsonIgnore]
        public string Slug => VehicleTypeFormattingUtils.GetSlugFromRussianName(Name);

        /// <summary>
        /// Отображаемое название (то же самое что Name, но для консистентности API)
        /// </summary>
        [JsonIgnore]
        public string DisplayName => Name;
    }
}