using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using WTExpCalc.Models;
using WTExpCalc.Shared;

namespace WTExpCalc.Services
{
    public class ExperienceApiService : IExperienceApi
    {
        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        private List<Node>? _lastFetchedNodesForDeps = null;
        private int _lastNationIdForDeps = -1;
        private int _lastTypeIdForDeps = -1;

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
        {
            List<Nation> nations = new();
            string url = "/api/nations";

            try
            {
                var response = await _http.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    nations = JsonSerializer.Deserialize<List<Nation>>(responseBody, _jsonOptions) ?? new();
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"ERROR: HttpRequestException: {httpEx.Message}");
                Console.WriteLine($"Status Code: {httpEx.StatusCode}");
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JsonException при парсинге ответа: {jsonEx.Message}");
                Console.WriteLine($"LineNumber: {jsonEx.LineNumber} | BytePositionInLine: {jsonEx.BytePositionInLine} | Path: {jsonEx.Path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Неожиданная ошибка: {ex}");
            }

            return nations;
        }

        public async Task<List<VehicleType>> GetVehicleTypesAsync()
            => await _http.GetFromJsonAsync<List<VehicleType>>("api/vehicle_types", _jsonOptions) ?? new();

        public async Task<Nation?> GetNationByIdAsync(int id)
        {
            try
            {
                var url = $"api/nations?id=eq.{id}&select=*&limit=1";
                var nationsList = await _http.GetFromJsonAsync<List<Nation>>(url, _jsonOptions);

                return nationsList?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching nation by ID {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<Nation?> GetNationBySlugAsync(string slug)
        {
            try
            {
                // Ищем нацию по английскому названию (slug)
                var url = $"api/nations?name=eq.{Uri.EscapeDataString(slug)}&select=*&limit=1";
                var nationsList = await _http.GetFromJsonAsync<List<Nation>>(url, _jsonOptions);

                return nationsList?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching nation by slug {slug}: {ex.Message}");
                return null;
            }
        }

        public async Task<VehicleType?> GetVehicleTypeBySlugAsync(string slug)
        {
            try
            {
                // Получаем все типы и ищем по slug
                var allTypes = await GetVehicleTypesAsync();
                return allTypes.FirstOrDefault(vt => vt.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching vehicle type by slug {slug}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<Node>> GetAllNodesFlatAsync(int nationId, int vehicleTypeId)
        {
            var url = $"api/nodes?nation_id=eq.{nationId}&vehicle_type_id=eq.{vehicleTypeId}&select=*";

            var nodes = await _http.GetFromJsonAsync<List<Node>>(url, _jsonOptions) ?? new List<Node>();

            _lastFetchedNodesForDeps = nodes;
            _lastNationIdForDeps = nationId;
            _lastTypeIdForDeps = vehicleTypeId;

            return nodes;
        }

        public async Task<List<NodeDependency>> GetAllDependenciesAsync(int nationId, int vehicleTypeId)
        {
            List<Node> relevantNodes;

            if (_lastNationIdForDeps == nationId && _lastTypeIdForDeps == vehicleTypeId && _lastFetchedNodesForDeps != null)
            {
                relevantNodes = _lastFetchedNodesForDeps;
            }
            else
            {
                relevantNodes = await GetAllNodesFlatAsync(nationId, vehicleTypeId);
            }

            if (relevantNodes == null || !relevantNodes.Any())
            {
                return new List<NodeDependency>();
            }

            var nodeIds = relevantNodes.Select(n => n.Id).ToList();
            if (!nodeIds.Any())
            {
                return new List<NodeDependency>();
            }

            var nodeIdFilter = string.Join(",", nodeIds);

            var url = $"api/node_dependencies?select=*&or=(node_id.in.({nodeIdFilter}),prerequisite_node_id.in.({nodeIdFilter}))";

            Console.WriteLine($"Fetching dependencies with URL: {url}");

            try
            {
                return await _http.GetFromJsonAsync<List<NodeDependency>>(url, _jsonOptions) ?? new List<NodeDependency>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching dependencies: {ex.Message}");
                return new List<NodeDependency>();
            }
        }

        public async Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId)
        {
            var url = $"api/rank_requirements?nation_id=eq.{nationId}&vehicle_type_id=eq.{vehicleTypeId}";
            return await _http.GetFromJsonAsync<List<RankRequirement>>(url, _jsonOptions) ?? new();
        }
    }
}