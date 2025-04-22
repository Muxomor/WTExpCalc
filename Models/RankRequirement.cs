using System.Text.Json.Serialization;

namespace WTExpCalc.Models
{
    public class RankRequirement
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nation_id")]
        public int NationId { get; set; }

        [JsonPropertyName("vehicle_type_id")]
        public int VehicleTypeId { get; set; }

        [JsonPropertyName("target_rank")]
        public int TargetRank { get; set; }

        [JsonPropertyName("previous_rank")]
        public int PreviousRank { get; set; }

        [JsonPropertyName("required_units")]
        public int RequiredUnits { get; set; }
    }
}