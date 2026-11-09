namespace WTExpCalc.Shared.Localization
{
    public sealed record LocalizedString(string Ru, string En);

    public static class UIStrings
    {
        public static readonly LocalizedString SelectNation = new("Выберите нацию", "Select Nation");
        public static readonly LocalizedString TechTreeFor = new("Ветка прокачки для нации", "Tech Tree for");
        public static readonly LocalizedString Loading = new("Загрузка...", "Loading...");
        public static readonly LocalizedString LoadingData = new("Загрузка данных…", "Loading data…");
        public static readonly LocalizedString Error = new("Ошибка", "Error");
        public static readonly LocalizedString Retry = new("Повторить", "Retry");
        public static readonly LocalizedString NoVehicleTypes = new("Нет доступных типов техники для данной нации.", "No vehicle types available for this nation.");
        public static readonly LocalizedString NoNationsAvailable = new("Нет доступных наций.", "No nations available.");
        public static readonly LocalizedString NoData = new("Нет данных для отображения.", "No data to display.");
        public static readonly LocalizedString Rank = new("Ранг", "Rank");
        public static readonly LocalizedString Selected = new("Выбрано", "Selected");
        public static readonly LocalizedString RP = new("ОИ", "RP");
        public static readonly LocalizedString SL = new("СЛ", "SL");
        public static readonly LocalizedString BR = new("БР", "BR");
        public static readonly LocalizedString CopyList = new("Копировать список", "Copy List");
        public static readonly LocalizedString CopyUrl = new("Копировать ссылку", "Copy Link");
        public static readonly LocalizedString DownloadScreenshot = new("Скриншот", "Screenshot");
        public static readonly LocalizedString PageNotFound = new("Страница не найдена", "Page Not Found");
        public static readonly LocalizedString TotalRP = new("Сумма опыта", "Total RP");
        public static readonly LocalizedString ReturnToHome = new("Вернуться на главную", "Return to Home");
        public static readonly LocalizedString RPLimit = new("Лимит ОИ", "RP Limit");
        public static readonly LocalizedString Unlimited = new("Без лимита", "Unlimited");
        public static readonly LocalizedString RPLimitExceededMessage = new("Нельзя выбрать больше техники - достигнут лимит опыта", "Cannot select more vehicles - RP limit reached");
        public static readonly LocalizedString PreparingScreenshot = new("Подготовка скриншота...", "Preparing screenshot...");
        public static readonly LocalizedString CreatingScreenshot = new("Создание скриншота...", "Creating screenshot...");
        public static readonly LocalizedString ProcessingScreenshot = new("Обработка изображения...", "Processing image...");
        public static readonly LocalizedString CopyingScreenshot = new("Копирование в буфер обмена...", "Copying to clipboard...");
        public static readonly LocalizedString ScreenshotCopied = new("Скриншот скопирован в буфер обмена", "Screenshot copied to clipboard");
        public static readonly LocalizedString PreparingDownload = new("Подготовка загрузки...", "Preparing download...");
        public static readonly LocalizedString ScreenshotSaved = new("Скриншот сохранен (буфер обмена недоступен)", "Screenshot saved (clipboard unavailable)");
        public static readonly LocalizedString ScreenshotError = new("Ошибка создания скриншота", "Screenshot creation error");
        public static readonly LocalizedString NationNotFound = new("Нация '{0}' не найдена.", "Nation '{0}' not found.");
        public static readonly LocalizedString VehicleTypesLoadFailed = new("Не удалось загрузить типы техники.", "Failed to load vehicle types.");
        public static readonly LocalizedString DataLoadError = new("Произошла ошибка при загрузке данных: {0}", "An error occurred while loading data: {0}");
        public static readonly LocalizedString AboutTitle = new("О проекте", "About");
        public static readonly LocalizedString AboutDisclaimer = new(
            "Этот сайт не принадлежит продавцу услуг прокачки {0}. Продавец не является владельцем, администратором или оператором этого сайта и не несёт ответственности за размещённую на нём информацию. Всегда перепроверяйте расчёты самостоятельно.",
            "This site does not belong to the boosting seller {0}. The seller is not the owner, administrator, or operator of this site and is not responsible for the information posted on it. Always double-check the calculations yourself.");
        public static readonly LocalizedString AboutPrivacy = new(
            "Проект не собирает данные о пользователях: не использует cookie, трекеры и иные средства сбора данных.",
            "The project does not collect user data: no cookies, trackers, or other data collection tools.");
        public static readonly LocalizedString AboutOpenSource = new(
            "Проект имеет открытый исходный код. Если вы заметили, что сайт не обновляется, вы можете запустить собственную копию и поддерживать её в актуальном состоянии. Ссылка на репозиторий - ниже.",
            "The project is open source. If you notice the site is not updating, you can run your own copy and keep it up to date. The repository link is below.");
        public static readonly LocalizedString AboutThanks = new(
            "Отдельная благодарность продавцу {0} за идеи, обратную связь и тестирование на ранних этапах проекта.",
            "Special thanks to the seller {0} for ideas, feedback, and testing in the early stages of the project.");
        public static readonly LocalizedString AboutRecommendation = new(
            "При заказе прокачки выбирайте {0}. Помните: цена ошибки - ваш аккаунт, а вместе с ним - время и деньги, потраченные на игру.",
            "When ordering boosting, choose {0}. Remember: the price of a mistake is your account, along with the time and money spent on the game.");
        public static readonly LocalizedString AboutVerifiedSellers = new("проверенных продавцов", "verified sellers");
        public static readonly LocalizedString AboutCanBeDisabled = new("Это окно можно отключить в настройках.", "This window can be disabled in settings.");
        public static readonly LocalizedString Close = new("Закрыть", "Close");
        public static readonly LocalizedString CalculatorTitle = new("Калькулятор", "Calculator");
        public static readonly LocalizedString CalculatorInsertXp = new("Добавить ОИ: {0}", "Add RP: {0}");
        public static readonly LocalizedString CalculatorCopyValue = new("Копировать значение", "Copy value");
        public static readonly LocalizedString CalculatorBackspace = new("Удалить символ", "Delete character");
        public static readonly LocalizedString CalculatorResize = new("Изменить размер", "Resize");

        public static readonly LocalizedString SettingsTitle = new("Настройки", "Settings");
        public static readonly LocalizedString SettingsAlignmentSection = new("Выравнивание", "Alignment");
        public static readonly LocalizedString SettingsTreeAlignment = new("Ветка прокачки", "Tech tree");
        public static readonly LocalizedString SettingsAlignmentHint = new(
            "При обычном масштабе ветка обычно занимает всю ширину экрана, поэтому разница может быть незаметна. Настройка полезна при уменьшении масштаба или на широком экране: она выбирает, где будет расположена ветка.",
            "At normal zoom the tech tree usually fills the screen width, so the difference may not be noticeable. This setting is useful when you zoom out or use a wide screen: it chooses where the tree sits.");
        public static readonly LocalizedString AlignLeft = new("Слева", "Left");
        public static readonly LocalizedString AlignCenter = new("По центру", "Center");
        public static readonly LocalizedString AlignRight = new("Справа", "Right");
        public static readonly LocalizedString SettingsBehaviorSection = new("Поведение", "Behavior");
        public static readonly LocalizedString SettingsShowAboutOnStartup = new("Показывать «О проекте» при запуске", "Show About on startup");
        public static readonly LocalizedString SettingsIncludePremiumInShareUrl = new("Добавлять премиумную технику в ссылку", "Include premium vehicles in share links");
        public static readonly LocalizedString SettingsScreenshotSection = new("Скриншоты", "Screenshots");
        public static readonly LocalizedString SettingsShowSummaryOnScreenshot = new("Показывать итоги на скриншоте", "Show summary on screenshot");
        public static readonly LocalizedString SettingsIncludePremiumInScreenshot = new("Показывать премиумную технику на скриншоте", "Show premium vehicles in screenshots");
        public static readonly LocalizedString SettingsReset = new("Сбросить настройки", "Reset settings");
        public static readonly LocalizedString SettingsSavedLocally = new("Настройки сохраняются в этом браузере.", "Settings are saved in this browser.");
    }
}
