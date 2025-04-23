// В файле IExperienceApi.cs
using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public interface IExperienceApi
    {
        Task<List<Nation>> GetNationsAsync();
        Task<List<VehicleType>> GetVehicleTypesAsync();

        /// <summary>
        /// Получает плоский список ВСЕХ узлов для заданной нации и типа техники.
        /// </summary>
        Task<List<Node>> GetAllNodesFlatAsync(int nationId, int vehicleTypeId);

        /// <summary>
        /// Получает ВСЕ зависимости (стрелки) для узлов, релевантных для заданной нации и типа техники.
        /// </summary>
        Task<List<NodeDependency>> GetAllDependenciesAsync(int nationId, int vehicleTypeId);
        Task<List<Node>> GetAllNodesAsync(int nationId, int vehicleTypeId); // Возможно, стоит удалить или переименовать, чтобы не путать
        Task<List<NodeDependency>> GetDependenciesAsync(int nodeId);
        Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId);
        Task<List<Node>> GetRootNodesAsync(int nationId, int vehicleTypeId);
        Task<List<Node>> GetChildNodesAsync(int parentNodeId);
    }
}