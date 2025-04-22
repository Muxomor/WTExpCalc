using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public class ExperienceApiService : IExperienceApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public ExperienceApiService(HttpClient httpClient)
        {
            _http = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            };
        }

        public async Task<List<Nation>> GetNationsAsync()
            => await _http.GetFromJsonAsync<List<Nation>>("nations", _jsonOptions) ?? new();

        public async Task<List<VehicleType>> GetVehicleTypesAsync()
            => await _http.GetFromJsonAsync<List<VehicleType>>("vehicle_types", _jsonOptions) ?? new();

        public async Task<List<Node>> GetAllNodesAsync(int nationId, int vehicleTypeId)
        {
            var url = $"nodes?nation_id={nationId}&vehicle_type_id={vehicleTypeId}";
            var nodes = await _http.GetFromJsonAsync<List<Node>>(url, _jsonOptions) ?? new();
            return BuildTree(nodes);
        }

        private List<Node> BuildTree(List<Node> nodes)
        {
            var nodeMap = nodes.ToDictionary(n => n.Id);
            var rootNodes = new List<Node>();

            foreach (var node in nodes)
            {
                if (node.ParentId.HasValue && nodeMap.TryGetValue(node.ParentId.Value, out var parent))
                {
                    parent.Children.Add(node);
                }
                else
                {
                    rootNodes.Add(node);
                }
            }
            return rootNodes.OrderBy(n => n.OrderInFolder).ToList();
        }

        public async Task<List<NodeDependency>> GetDependenciesAsync(int nodeId)
        {
            var url = $"node_dependencies?node_id={nodeId}";
            return await _http.GetFromJsonAsync<List<NodeDependency>>(url, _jsonOptions) ?? new();
        }

        public async Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId)
        {
            var url = $"rank_requirements?nation_id={nationId}&vehicle_type_id={vehicleTypeId}";
            return await _http.GetFromJsonAsync<List<RankRequirement>>(url, _jsonOptions) ?? new();
        }
    }
}