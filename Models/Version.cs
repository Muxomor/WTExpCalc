using System.Text.Json.Serialization;

namespace WTExpCalc.Models
{
    public class Version
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("version_number")]
        public string VersionNumber { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}