using System.Text.Json.Serialization;

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
    }
}