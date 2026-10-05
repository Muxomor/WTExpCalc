namespace WTExpCalc.Shared
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
        public static readonly LocalizedString Rank = new("Ранг", "Rank");
        public static readonly LocalizedString Selected = new("Выбрано", "Selected");
        public static readonly LocalizedString RP = new("ОИ", "RP");
        public static readonly LocalizedString SL = new("СЛ", "SL");
        public static readonly LocalizedString BR = new("БР", "BR");
        public static readonly LocalizedString CopyList = new("Копировать список", "Copy List");
        public static readonly LocalizedString CopyUrl = new("Копировать ссылку", "Copy Link");
        public static readonly LocalizedString CopyScreenshot = new("Копировать скриншот в буфер обмена", "Copy Screenshot to Clipboard");
        public static readonly LocalizedString CopyScreenshotHttp = new("Копировать скриншот в буфер обмена (HTTP)", "Copy Screenshot to Clipboard (HTTP)");
        public static readonly LocalizedString DownloadScreenshot = new("Скачать скриншот", "Download Screenshot");
        public static readonly LocalizedString PageNotFound = new("Страница не найдена", "Page Not Found");
        public static readonly LocalizedString TotalRP = new("Сумма опыта", "Total RP");
        public static readonly LocalizedString ReturnToHome = new("Вернуться на главную", "Return to Home");
        public static readonly LocalizedString RPLimit = new("Лимит ОИ", "RP Limit");
        public static readonly LocalizedString Unlimited = new("Без лимита", "Unlimited");
        public static readonly LocalizedString RPLimitExceeded = new("Превышен лимит опыта!", "RP limit exceeded!");
        public static readonly LocalizedString RPLimitExceededMessage = new("Нельзя выбрать больше техники - достигнут лимит опыта", "Cannot select more vehicles - RP limit reached");
        public static readonly LocalizedString Screenshot4KTooltip = new("Скриншот 4K (максимум 4000×4000)", "4000x4000 Screenshot (max 4000×4000)");
        public static readonly LocalizedString Screenshot4KTooltipHttp = new("Скриншот 4K (максимум 4000×4000) (HTTP)", "4000x4000 Screenshot (max 4000×4000) (HTTP)");
        public static readonly LocalizedString Preparing4KScreenshot = new("Подготовка 4K скриншота...", "Preparing 4K screenshot...");
        public static readonly LocalizedString Creating4KScreenshot = new("Создание 4K скриншота...", "Creating 4K screenshot...");
        public static readonly LocalizedString Processing4KImage = new("Обработка 4K изображения...", "Processing 4K image...");
        public static readonly LocalizedString Preparing4KDownload = new("Подготовка 4K загрузки...", "Preparing 4K download...");
        public static readonly LocalizedString Screenshot4KSaved = new("4K скриншот сохранен", "4K screenshot saved");
        public static readonly LocalizedString Error4KScreenshot = new("Ошибка создания 4K скриншота", "4K screenshot creation error");
        public static readonly LocalizedString PreparingScreenshot = new("Подготовка скриншота...", "Preparing screenshot...");
        public static readonly LocalizedString ScreenshotError = new("Ошибка создания скриншота", "Screenshot creation error");
        public static readonly LocalizedString NationNotFound = new("Нация '{0}' не найдена.", "Nation '{0}' not found.");
        public static readonly LocalizedString VehicleTypesLoadFailed = new("Не удалось загрузить типы техники.", "Failed to load vehicle types.");
        public static readonly LocalizedString DataLoadError = new("Произошла ошибка при загрузке данных: {0}", "An error occurred while loading data: {0}");
        public static readonly LocalizedString NavigationError = new("Ошибка навигации: {0}", "Navigation error: {0}");
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
        public static readonly LocalizedString Close = new("Закрыть", "Close");
    }
}
