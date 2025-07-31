using WTExpCalc.Models;
using WTExpCalc.Services;
using WTExpCalc.Shared;

namespace WTExpCalc.Utils
{
    /// <summary>
    /// Утилиты для отладки маршрутизации
    /// </summary>
    public static class DebuggingHelper
    {
        public static void LogNationData(List<Nation>? nations)
        {
            if (nations == null)
            {
                Console.WriteLine("Nations list is null");
                return;
            }

            Console.WriteLine($"=== Nations Debug Info ({nations.Count} nations) ===");
            foreach (var nation in nations)
            {
                Console.WriteLine($"Nation: '{nation.Name}' -> Slug: '{nation.Slug}' -> Display: '{nation.DisplayName}'");
            }
        }

        public static void LogVehicleTypeData(List<VehicleType>? vehicleTypes)
        {
            if (vehicleTypes == null)
            {
                Console.WriteLine("VehicleTypes list is null");
                return;
            }

            Console.WriteLine($"=== Vehicle Types Debug Info ({vehicleTypes.Count} types) ===");
            foreach (var vt in vehicleTypes)
            {
                Console.WriteLine($"VehicleType: '{vt.Name}' -> Slug: '{vt.Slug}' -> Display: '{vt.DisplayName}'");
            }
        }

        public static void LogSlugMappings()
        {
            Console.WriteLine("=== Vehicle Type Slug Mappings ===");
            var allSlugs = VehicleTypeFormattingUtils.GetAllValidSlugs();
            foreach (var slug in allSlugs)
            {
                var displayName = VehicleTypeFormattingUtils.GetDisplayNameFromSlug(slug);
                Console.WriteLine($"Slug: '{slug}' -> Display: '{displayName}'");
            }

            Console.WriteLine("=== Nation Slug Mappings ===");
            var nationSlugs = FormattingUtils.GetAllValidNationSlugs();
            foreach (var slug in nationSlugs)
            {
                var displayName = FormattingUtils.FormatNationName(slug);
                Console.WriteLine($"Slug: '{slug}' -> Display: '{displayName}'");
            }
        }

        public static void TestSlugConversion(string testRussianName)
        {
            Console.WriteLine($"=== Testing Slug Conversion for '{testRussianName}' ===");
            var slug = VehicleTypeFormattingUtils.GetSlugFromRussianName(testRussianName);
            var backToRussian = VehicleTypeFormattingUtils.GetDisplayNameFromSlug(slug);
            var isValid = VehicleTypeFormattingUtils.IsValidSlug(slug);

            Console.WriteLine($"Russian: '{testRussianName}' -> Slug: '{slug}' -> Back to Russian: '{backToRussian}' -> Valid: {isValid}");
        }
    }
}