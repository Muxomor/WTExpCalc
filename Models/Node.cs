using System.Text.Json.Serialization;

namespace WTExpCalc.Models
{
    public class Node
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("external_id")]
        public string ExternalId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "vehicle";

        [JsonPropertyName("tech_category")]
        public string TechCategory { get; set; } = "standard";

        [JsonPropertyName("nation_id")]
        public int NationId { get; set; }

        [JsonPropertyName("vehicle_type_id")]
        public int VehicleTypeId { get; set; }

        [JsonPropertyName("parent_id")]
        public int? ParentId { get; set; }

        [JsonIgnore]
        public Node? ParentNode { get; set; } // вот это добавили

        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("silver_cost")]
        public int? SilverCost { get; set; }

        [JsonPropertyName("required_exp")]
        public int? RequiredExp { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("br")]
        public decimal? BattleRating { get; set; }

        // Для удобства в Razor привяжем короткое имя
        [JsonIgnore]
        public decimal? Br => BattleRating;

        [JsonPropertyName("column_index")]
        public int? ColumnIndex { get; set; }

        [JsonPropertyName("row_index")]
        public int? RowIndex { get; set; }

        [JsonPropertyName("order_in_folder")]
        public int? OrderInFolder { get; set; }

        public List<Node> Children { get; set; } = new();
    }
}
