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
                ["TotalRP"] = "Сумма опыта",
                ["ReturnToHome"] = "Вернуться на главную",
                ["RPLimit"] = "Лимит ОИ",
                ["Unlimited"] = "Без лимита",
                ["RPLimitExceeded"] = "Превышен лимит опыта!",
                ["RPLimitExceededMessage"] = "Нельзя выбрать больше техники - достигнут лимит опыта",
                ["Screenshot4KTooltip"] = "Скриншот 4K (максимум 4000×4000)",
                ["Screenshot4KTooltipHTTP"] = "Скриншот 4K (максимум 4000×4000) (HTTP)",
                ["Preparing4KScreenshot"] = "Подготовка 4K скриншота...",
                ["Creating4KScreenshot"] = "Создание 4K скриншота...",
                ["Processing4KImage"] = "Обработка 4K изображения...",
                ["Preparing4KDownload"] = "Подготовка 4K загрузки...",
                ["Screenshot4KSaved"] = "4K скриншот сохранен",
                ["Error4KScreenshot"] = "Ошибка создания 4K скриншота"

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
                ["TotalRP"] = "Total RP",
                ["ReturnToHome"] = "Return to Home",
                ["RPLimit"] = "RP Limit",
                ["Unlimited"] = "Unlimited",
                ["RPLimitExceeded"] = "RP limit exceeded!",
                ["RPLimitExceededMessage"] = "Cannot select more vehicles - RP limit reached",
                ["Screenshot4KTooltip"] = "4000x4000 Screenshot (max 4000×4000)",
                ["Screenshot4KTooltipHTTP"] = "4000x4000 Screenshot (max 4000×4000) (HTTP)",
                ["Preparing4KScreenshot"] = "Preparing 4K screenshot...",
                ["Creating4KScreenshot"] = "Creating 4K screenshot...",
                ["Processing4KImage"] = "Processing 4K image...",
                ["Preparing4KDownload"] = "Preparing 4K download...",
                ["Screenshot4KSaved"] = "4K screenshot saved",
                ["Error4KScreenshot"] = "4K screenshot creation error"

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