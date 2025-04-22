using System.Collections.Generic;
using System.Threading.Tasks;
using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public interface IExperienceApi
    {
        Task<List<Nation>> GetNationsAsync();
        Task<List<VehicleType>> GetVehicleTypesAsync();
        Task<List<Node>> GetAllNodesAsync(int nationId, int vehicleTypeId);
        Task<List<NodeDependency>> GetDependenciesAsync(int nodeId);
        Task<List<RankRequirement>> GetRankRequirementsAsync(int nationId, int vehicleTypeId);
    }
}