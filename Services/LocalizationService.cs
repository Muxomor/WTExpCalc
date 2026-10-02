using System.Globalization;
using Microsoft.JSInterop;
using WTExpCalc.Shared;

namespace WTExpCalc.Services
{
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        bool IsEnglish { get; }
        string Text(LocalizedString text);
        string Text(LocalizedString text, params object[] args);
        string NationName(string name, int? id = null);
        string VehicleTypeName(string russianName, string? englishName);
        string NodeName(string russianName, string? englishName);
        Task SetLanguageAsync(string language);
        Task<string> LoadSavedLanguageAsync();
        event Action? LanguageChanged;
    }

    public class LocalizationService : ILocalizationService
    {
        private readonly IJSRuntime _jsRuntime;
        private string _currentLanguage = "ru";

        public string CurrentLanguage => _currentLanguage;
        public bool IsEnglish => _currentLanguage == "en";
        public event Action? LanguageChanged;

        public LocalizationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string Text(LocalizedString text) => IsEnglish ? text.En : text.Ru;

        public string Text(LocalizedString text, params object[] args) => string.Format(Text(text), args);

        public string NationName(string name, int? id = null)
            => GameDataLocalization.NationName(name, IsEnglish, id);

        public string VehicleTypeName(string russianName, string? englishName)
            => GameDataLocalization.VehicleTypeName(russianName, englishName, IsEnglish);

        public string NodeName(string russianName, string? englishName)
            => GameDataLocalization.NodeName(russianName, englishName, IsEnglish);

        public async Task SetLanguageAsync(string language)
        {
            if (language != "ru" && language != "en")
                language = "ru";

            ApplyCulture(language);

            if (_currentLanguage == language)
                return;

            _currentLanguage = language;

            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "selectedLanguage", language);
            }
            catch
            {
                /* localStorage may be unavailable in some contexts */
            }

            LanguageChanged?.Invoke();
        }

        public async Task<string> LoadSavedLanguageAsync()
        {
            try
            {
                var saved = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "selectedLanguage");
                return saved is "en" or "ru" ? saved : "ru";
            }
            catch
            {
                return "ru";
            }
        }

        private static void ApplyCulture(string language)
        {
            var culture = CultureInfo.GetCultureInfo(language == "en" ? "en-US" : "ru-RU");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public static void PreloadCultures()
        {
            foreach (var name in new[] { "ru-RU", "en-US" })
            {
                var culture = CultureInfo.GetCultureInfo(name);
                _ = culture.NumberFormat;
                _ = culture.DateTimeFormat;
            }
        }
    }
}
