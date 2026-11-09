using Microsoft.Extensions.Configuration;

namespace WTExpCalc.Services.Features
{
    public interface IFeatureFlags
    {
        bool CalculatorEnabled { get; }
    }

    public sealed class FeatureFlags : IFeatureFlags
    {
        public FeatureFlags(IConfiguration configuration)
        {
            CalculatorEnabled = bool.TryParse(
                configuration["Features:CalculatorEnabled"],
                out var enabled) && enabled;
        }

        public bool CalculatorEnabled { get; }
    }
}
