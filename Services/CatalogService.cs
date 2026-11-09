using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public interface ICatalogService
    {
        Task<List<Nation>> GetNationsAsync();
        Task<List<VehicleType>> GetVehicleTypesAsync();
        Task<Nation?> GetNationBySlugAsync(string slug);
    }

    /// <summary>
    /// Держит справочники наций и типов техники в памяти на время жизни приложения.
    /// Справочники меняются только вместе с деплоем данных, поэтому повторно их не запрашиваем.
    /// </summary>
    public sealed class CatalogService : ICatalogService
    {
        private readonly IExperienceApi _api;
        private Task<List<Nation>>? _nationsTask;
        private Task<List<VehicleType>>? _vehicleTypesTask;

        public CatalogService(IExperienceApi api)
        {
            _api = api;
        }

        public Task<List<Nation>> GetNationsAsync()
            => _nationsTask ??= LoadNationsAsync();

        public Task<List<VehicleType>> GetVehicleTypesAsync()
            => _vehicleTypesTask ??= LoadVehicleTypesAsync();

        public async Task<Nation?> GetNationBySlugAsync(string slug)
        {
            var nations = await GetNationsAsync();
            return nations.FirstOrDefault(n => n.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<Nation>> LoadNationsAsync()
        {
            try
            {
                return await _api.GetNationsAsync();
            }
            catch
            {
                // Не кэшируем неудачную попытку: следующий вызов должен повторить запрос.
                _nationsTask = null;
                throw;
            }
        }

        private async Task<List<VehicleType>> LoadVehicleTypesAsync()
        {
            try
            {
                return await _api.GetVehicleTypesAsync();
            }
            catch
            {
                _vehicleTypesTask = null;
                throw;
            }
        }
    }
}
