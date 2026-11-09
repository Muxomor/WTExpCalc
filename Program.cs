using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WTExpCalc;
using WTExpCalc.Services.Api;
using WTExpCalc.Services.Features;
using WTExpCalc.Services.Localization;
using WTExpCalc.Services.Settings;
using WTExpCalc.Services.Tree;
using WTExpCalc.Services.UrlState;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(_ =>
{
    var httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    if (builder.HostEnvironment.IsDevelopment())
    {
        var backendUrl = builder.Configuration["BackendApi:BaseUrl"];
        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            throw new InvalidOperationException(
                "BackendApi:BaseUrl is not configured. Set it in wwwroot/appsettings.Development.json.");
        }

        if (!backendUrl.EndsWith('/'))
        {
            backendUrl += "/";
        }

        httpClient.BaseAddress = new Uri(backendUrl);
    }
    else
    {
        httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    }

    return httpClient;
});

builder.Services.AddScoped<IExperienceApi, ExperienceApiService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddSingleton<IFeatureFlags, FeatureFlags>();
builder.Services.AddScoped<TreeSummaryFormatter>();
builder.Services.AddScoped<IUrlStateManager, UrlStateManager>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();
builder.Services.AddScoped<IUiSettingsService, UiSettingsService>();

await builder.Build().RunAsync();
