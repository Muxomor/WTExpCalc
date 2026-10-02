using System.Text.Json.Serialization;

namespace WTExpCalc.Models
{
    public class Nation
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("name_eng")]
        public string? NameEnglish { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonIgnore]
        public string Slug => Name.ToLowerInvariant();
    }
}