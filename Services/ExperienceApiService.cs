using System.Net.Http.Json;
using System.Text.Json;
using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public sealed class ExperienceApiService : IExperienceApi
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        private readonly HttpClient _http;

        public ExperienceApiService(HttpClient httpClient)
        {
            _http = httpClient;
        }

        public async Task<List<Nation>> GetNationsAsync()
            => await _http.GetFromJsonAsync<List<Nation>>("api/nations?select=*&order=id", JsonOptions) ?? new();

        public async Task<List<VehicleType>> GetVehicleTypesAsync()
            => await _http.GetFromJsonAsync<List<VehicleType>>("api/vehicle_types?select=*&order=id", JsonOptions) ?? new();

        public async Task<List<Node>> GetAllNodesFlatAsync(int nationId, int vehicleTypeId)
        {
            var url = $"api/nodes?nation_id=eq.{nationId}&vehicle_type_id=eq.{vehicleTypeId}&select=*";
            return await _http.GetFromJsonAsync<List<Node>>(url, JsonOptions) ?? new();
        }

        public async Task<List<NodeDependency>> GetAllDependenciesAsync(IReadOnlyCollection<int> nodeIds)
        {
            if (nodeIds.Count == 0)
            {
                return new();
            }

            var nodeIdFilter = string.Join(",", nodeIds);
            var url = $"api/node_dependencies?select=*&or=(node_id.in.({nodeIdFilter}),prerequisite_node_id.in.({nodeIdFilter}))";

            return await _http.GetFromJsonAsync<List<NodeDependency>>(url, JsonOptions) ?? new();
        }

        public async Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId)
        {
            var url = $"api/rank_requirements?nation_id=eq.{nationId}&vehicle_type_id=eq.{vehicleTypeId}&select=*";
            return await _http.GetFromJsonAsync<List<RankRequirement>>(url, JsonOptions) ?? new();
        }

        public async Task<AppVersion?> GetCurrentVersionAsync()
        {
            var url = "api/version?order=created_at.desc&limit=1";
            var versions = await _http.GetFromJsonAsync<List<AppVersion>>(url, JsonOptions);
            return versions?.FirstOrDefault();
        }
    }
}
