using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public interface IExperienceApi
    {
        Task<List<Nation>> GetNationsAsync();
        Task<Nation?> GetNationByIdAsync(int id);
        Task<Nation?> GetNationBySlugAsync(string slug);
        Task<List<VehicleType>> GetVehicleTypesAsync();
        Task<VehicleType?> GetVehicleTypeBySlugAsync(string slug);

        Task<List<Node>> GetAllNodesFlatAsync(int nationId, int vehicleTypeId);
        Task<List<NodeDependency>> GetAllDependenciesAsync(int nationId, int vehicleTypeId);
        Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId);
    }
}