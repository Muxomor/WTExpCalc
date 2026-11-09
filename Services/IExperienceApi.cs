using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public interface IExperienceApi
    {
        Task<List<Nation>> GetNationsAsync();
        Task<List<VehicleType>> GetVehicleTypesAsync();

        Task<List<Node>> GetAllNodesFlatAsync(int nationId, int vehicleTypeId);
        Task<List<NodeDependency>> GetAllDependenciesAsync(IReadOnlyCollection<int> nodeIds);
        Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId);
        Task<AppVersion?> GetCurrentVersionAsync();
    }
}
