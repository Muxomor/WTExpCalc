using System.Text.Json.Serialization;

namespace WTExpCalc.Models
{
    public class NodeDependency
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("node_id")]
        public int NodeId { get; set; }

        [JsonPropertyName("prerequisite_node_id")]
        public int PrerequisiteNodeId { get; set; }
    }
}