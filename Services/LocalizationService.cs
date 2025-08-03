using System.Globalization;
using Microsoft.JSInterop;

namespace WTExpCalc.Services
{
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }
        bool IsEnglish { get; }
        string GetText(string key);
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

        // Простой словарь переводов
        private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
        {
            ["ru"] = new Dictionary<string, string>
            {
                ["SelectNation"] = "Выберите нацию",
                ["TechTreeFor"] = "Ветка прокачки для нации",
                ["Loading"] = "Загрузка...",
                ["LoadingData"] = "Загрузка данных…",
                ["Error"] = "Ошибка",
                ["Retry"] = "Повторить",
                ["NoVehicleTypes"] = "Нет доступных типов техники для данной нации.",
                ["Rank"] = "Ранг",
                ["Selected"] = "Выбрано",
                ["RP"] = "ОИ",
                ["SL"] = "СЛ",
                ["BR"] = "БР",
                ["CopyList"] = "Копировать список",
                ["CopyUrl"] = "Копировать ссылку",
                ["CopyScreenshot"] = "Скопировать скриншот в буфер обмена",
                ["DownloadScreenshot"] = "Скачать скриншот",
                ["PageNotFound"] = "Страница не найдена",
                ["ReturnToHome"] = "Вернуться на главную"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["SelectNation"] = "Select Nation",
                ["TechTreeFor"] = "Tech Tree for",
                ["Loading"] = "Loading...",
                ["LoadingData"] = "Loading data…",
                ["Error"] = "Error",
                ["Retry"] = "Retry",
                ["NoVehicleTypes"] = "No vehicle types available for this nation.",
                ["Rank"] = "Rank",
                ["Selected"] = "Selected",
                ["RP"] = "RP",
                ["SL"] = "SL",
                ["BR"] = "BR",
                ["CopyList"] = "Copy List",
                ["CopyUrl"] = "Copy Link",
                ["CopyScreenshot"] = "Copy Screenshot to Clipboard",
                ["DownloadScreenshot"] = "Download Screenshot",
                ["PageNotFound"] = "Page Not Found",
                ["ReturnToHome"] = "Return to Home"
            }
        };

        public LocalizationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string GetText(string key)
        {
            if (_translations.TryGetValue(_currentLanguage, out var languageDict) &&
                languageDict.TryGetValue(key, out var translation))
            {
                return translation;
            }

            // Fallback к русскому
            if (_currentLanguage != "ru" &&
                _translations.TryGetValue("ru", out var ruDict) &&
                ruDict.TryGetValue(key, out var ruTranslation))
            {
                return ruTranslation;
            }

            return $"[{key}]";
        }

        public async Task SetLanguageAsync(string language)
        {
            if (language != "ru" && language != "en")
                language = "ru";

            if (_currentLanguage != language)
            {
                _currentLanguage = language;

                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "selectedLanguage", language);
                }
                catch { }

                LanguageChanged?.Invoke();
            }
        }

        public async Task<string> LoadSavedLanguageAsync()
        {
            try
            {
                var saved = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "selectedLanguage");
                return (saved == "en" || saved == "ru") ? saved : "ru";
            }
            catch
            {
                return "ru";
            }
        }
    }
}