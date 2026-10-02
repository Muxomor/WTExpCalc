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

        [JsonPropertyName("name_eng")]
        public string? NameEnglish { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonIgnore]
        public string Slug => GameDataLocalization.VehicleTypeSlug(Name);
    }
}
