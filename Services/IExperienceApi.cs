using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public interface IExperienceApi
    {
        Task<List<Nation>> GetNationsAsync();
        Task<Nation?> GetNationByIdAsync(int id);
        Task<List<VehicleType>> GetVehicleTypesAsync();

        Task<List<Node>> GetAllNodesFlatAsync(int nationId, int vehicleTypeId);

        Task<List<NodeDependency>> GetAllDependenciesAsync(int nationId, int vehicleTypeId);
        //Task<List<Node>> GetAllNodesAsync(int nationId, int vehicleTypeId);
        //Task<List<NodeDependency>> GetDependenciesAsync(int nodeId);
        Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId);
        //Task<List<Node>> GetRootNodesAsync(int nationId, int vehicleTypeId);
        //Task<List<Node>> GetChildNodesAsync(int parentNodeId);
    }
}