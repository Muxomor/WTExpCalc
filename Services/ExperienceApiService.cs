// В файле ExperienceApiService.cs
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

        // Переменная для кэширования плоского списка узлов внутри одного запроса зависимостей
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
            string url = "/api/nations"; // Правильный URL с префиксом
            Console.WriteLine($"[GetNationsAsync] LOG: Попытка вызова URL: {url}"); // Лог ПЕРЕД вызовом

            try
            {
                // Используем GetAsync чтобы разделить получение ответа и десериализацию
                var response = await _http.GetAsync(url);
                Console.WriteLine($"[GetNationsAsync] LOG: Статус код ответа: {response.StatusCode}"); // Лог статус кода

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Логируем только часть ответа, чтобы не засорять консоль
                    Console.WriteLine($"[GetNationsAsync] LOG: Получен ответ (сырой, начало): {responseBody.Substring(0, Math.Min(responseBody.Length, 300))}...");

                    Console.WriteLine("[GetNationsAsync] LOG: Попытка десериализации...");
                    nations = JsonSerializer.Deserialize<List<Nation>>(responseBody, _jsonOptions) ?? new();
                    Console.WriteLine($"[GetNationsAsync] LOG: Десериализация успешна. Получено наций: {nations.Count}");
                }
                else
                {
                    // Логируем тело ошибки, если статус код не успешный
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GetNationsAsync] LOG: HTTP Ошибка {response.StatusCode}. Тело ответа: {errorBody}");
                }
            }
            catch (HttpRequestException httpEx) // Ловим ошибки сети/HTTP
            {
                Console.WriteLine($"[GetNationsAsync] ERROR: HttpRequestException: {httpEx.Message}");
                Console.WriteLine($"---> Status Code: {httpEx.StatusCode}"); // Попробуем вывести статус код, если есть
            }
            catch (JsonException jsonEx) // Ловим именно ошибки парсинга JSON
            {
                Console.WriteLine($"[GetNationsAsync] ERROR: JsonException при парсинге ответа: {jsonEx.Message}");
                Console.WriteLine($"---> LineNumber: {jsonEx.LineNumber} | BytePositionInLine: {jsonEx.BytePositionInLine} | Path: {jsonEx.Path}");
            }
            catch (Exception ex) // Ловим любые другие ошибки
            {
                Console.WriteLine($"[GetNationsAsync] ERROR: Неожиданная ошибка: {ex}");
            }

            Console.WriteLine("[GetNationsAsync] LOG: Метод завершен.");
            return nations;
        }

        public async Task<List<VehicleType>> GetVehicleTypesAsync()
            => await _http.GetFromJsonAsync<List<VehicleType>>("api/vehicle_types", _jsonOptions) ?? new();


        /// <summary>
        /// Получает плоский список ВСЕХ узлов для заданной нации и типа техники.
        /// НЕ строит иерархию здесь.
        /// </summary>
        public async Task<List<Node>> GetAllNodesFlatAsync(int nationId, int vehicleTypeId)
        {
            // URL для вашего API (PostgREST?). Загружаем все поля узла.
            // Возможно, стоит добавить select=*, если нужны все поля из Node класса.
            var url = $"api/nodes?nation_id=eq.{nationId}&vehicle_type_id=eq.{vehicleTypeId}&select=*";

            // Кэшируем результат для возможного использования в GetAllDependenciesAsync
            var nodes = await _http.GetFromJsonAsync<List<Node>>(url, _jsonOptions) ?? new List<Node>();

            _lastFetchedNodesForDeps = nodes;
            _lastNationIdForDeps = nationId;
            _lastTypeIdForDeps = vehicleTypeId;

            return nodes;
        }

        /// <summary>
        /// Получает ВСЕ зависимости (стрелки) для узлов, релевантных для заданной нации и типа техники.
        /// Использует предварительно загруженные узлы (если возможно) для получения их ID.
        /// </summary>
        public async Task<List<NodeDependency>> GetAllDependenciesAsync(int nationId, int vehicleTypeId)
        {
            List<Node> relevantNodes;

            // Проверяем, есть ли у нас уже загруженные узлы для этой комбинации нации/типа
            if (_lastNationIdForDeps == nationId && _lastTypeIdForDeps == vehicleTypeId && _lastFetchedNodesForDeps != null)
            {
                relevantNodes = _lastFetchedNodesForDeps;
            }
            else
            {
                // Если нет, загружаем их снова (менее эффективно, но необходимо)
                relevantNodes = await GetAllNodesFlatAsync(nationId, vehicleTypeId);
            }

            if (relevantNodes == null || !relevantNodes.Any())
            {
                return new List<NodeDependency>(); // Нет узлов - нет зависимостей
            }

            // Получаем список ID всех релевантных узлов
            var nodeIds = relevantNodes.Select(n => n.Id).ToList();
            if (!nodeIds.Any())
            {
                return new List<NodeDependency>();
            }

            // Формируем строку ID для запроса PostgREST "in.(id1,id2,...)"
            var nodeIdFilter = string.Join(",", nodeIds);

            // Формируем URL для PostgREST:
            // Ищем зависимости, где ЛИБО node_id из нашего списка, ЛИБО prerequisite_node_id из нашего списка
            var url = $"api/node_dependencies?select=*&or=(node_id.in.({nodeIdFilter}),prerequisite_node_id.in.({nodeIdFilter}))";

            Console.WriteLine($"Fetching dependencies with URL: {url}"); // Для отладки

            try
            {
                return await _http.GetFromJsonAsync<List<NodeDependency>>(url, _jsonOptions) ?? new List<NodeDependency>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching dependencies: {ex.Message}");
                // В зависимости от требований, можно вернуть пустой список или пробросить исключение
                return new List<NodeDependency>();
            }
        }

        public async Task<List<Node>> GetAllNodesAsync(int nationId, int vehicleTypeId)
        {
            var url = $"api/nodes?nation_id=eq.{nationId}&vehicle_type_id=eq.{vehicleTypeId}&select=*";
            var nodes = await _http.GetFromJsonAsync<List<Node>>(url, _jsonOptions) ?? new List<Node>();

            // Возвращаем корневые узлы построенного дерева
            return BuildTree(nodes);
        }

        /// <summary>
        /// Строит иерархию узлов на основе ParentId из плоского списка.
        /// Возвращает список корневых узлов.
        /// </summary>
        private List<Node> BuildTree(List<Node> nodes)
        {
            var nodeMap = nodes.ToDictionary(n => n.Id);
            var rootNodes = new List<Node>();

            foreach (var node in nodes)
            {
                // Важно очищать Children перед построением, если объект Node используется повторно
                node.Children.Clear();
                node.ParentNode = null; // Сбрасываем ссылку на родителя

                if (node.ParentId.HasValue && nodeMap.TryGetValue(node.ParentId.Value, out var parent))
                {
                    // Добавляем ссылку на родителя и ребенка друг к другу
                    node.ParentNode = parent;
                    parent.Children.Add(node);
                }
                else
                {
                    rootNodes.Add(node); // Узел без родителя - корень
                }
            }
            // Опциональная сортировка корневых узлов (возможно, лучше сортировать в компоненте)
            // return rootNodes.OrderBy(n => n.Rank).ThenBy(n => n.ColumnIndex ?? 0).ToList();
            return rootNodes;
        }

        public async Task<List<NodeDependency>> GetDependenciesAsync(int nodeId)
        {
            var url = $"api/node_dependencies?node_id=eq.{nodeId}";
            return await _http.GetFromJsonAsync<List<NodeDependency>>(url, _jsonOptions) ?? new();
        }

        public async Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId)
        {
            var url = $"api/rank_requirements?nation_id=eq.{nationId}&vehicle_type_id=eq.{vehicleTypeId}";
            return await _http.GetFromJsonAsync<List<RankRequirement>>(url, _jsonOptions) ?? new();
        }
        public async Task<List<Node>> GetRootNodesAsync(int nationId, int vehicleTypeId)
        {
            // Этот метод может быть полезен, если вам нужны ТОЛЬКО корневые узлы
            var url = $"api/nodes?nation_id=eq.{nationId}&vehicle_type_id=eq.{vehicleTypeId}&parent_id=is.null&select=*";
            return await _http.GetFromJsonAsync<List<Node>>(url, _jsonOptions) ?? new();
        }

        public async Task<List<Node>> GetChildNodesAsync(int parentNodeId)
        {
            var url = $"api/nodes?parent_id=eq.{parentNodeId}&select=*";
            return await _http.GetFromJsonAsync<List<Node>>(url, _jsonOptions) ?? new();
        }
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
    }
}