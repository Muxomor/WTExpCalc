using WTExpCalc.Models;
using WTExpCalc.Shared;

namespace WTExpCalc.Services
{
    /// <summary>
    /// Сервис для работы с URL и маршрутизацией
    /// </summary>
    public interface IUrlHelperService
    {
        /// <summary>
        /// Строит URL для нации и типа техники
        /// </summary>
        string BuildTreeUrl(string nationSlug, string vehicleTypeSlug);

        /// <summary>
        /// Строит URL для нации (с первым доступным типом техники)
        /// </summary>
        Task<string> BuildTreeUrlAsync(string nationSlug);

        /// <summary>
        /// Валидирует параметры URL
        /// </summary>
        bool ValidateUrlParams(string nationSlug, string? vehicleTypeSlug = null);
    }

    public class UrlHelperService : IUrlHelperService
    {
        private readonly IExperienceApi _api;

        public UrlHelperService(IExperienceApi api)
        {
            _api = api;
        }

        public string BuildTreeUrl(string nationSlug, string vehicleTypeSlug)
        {
            return $"/tree/{nationSlug}/{vehicleTypeSlug}";
        }

        public async Task<string> BuildTreeUrlAsync(string nationSlug)
        {
            try
            {
                var vehicleTypes = await _api.GetVehicleTypesAsync();
                if (vehicleTypes != null && vehicleTypes.Any())
                {
                    var firstType = vehicleTypes.First();
                    return BuildTreeUrl(nationSlug, firstType.Slug);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building tree URL for nation {nationSlug}: {ex.Message}");
            }

            // Fallback
            return $"/tree/{nationSlug}";
        }

        public bool ValidateUrlParams(string nationSlug, string? vehicleTypeSlug = null)
        {
            // Валидация нации
            if (!FormattingUtils.IsValidNationSlug(nationSlug))
            {
                return false;
            }

            // Валидация типа техники (если указан)
            if (!string.IsNullOrEmpty(vehicleTypeSlug) &&
                !VehicleTypeFormattingUtils.IsValidSlug(vehicleTypeSlug))
            {
                return false;
            }

            return true;
        }
    }
}