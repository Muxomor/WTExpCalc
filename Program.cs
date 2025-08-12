using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WTExpCalc;
using WTExpCalc.Services;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => {
    var httpClient = new HttpClient();
    if (builder.HostEnvironment.IsDevelopment())
    {
        var backendUrl = builder.Configuration["BackendApi:BaseUrl"];
        if (string.IsNullOrEmpty(backendUrl))
        {
            backendUrl = "http://192.168.0.105:8881/";
            Console.WriteLine($"URL Traefik ��� ���������� �� ������, ������������ ���������: {backendUrl}");
        }
        else
        {
            Console.WriteLine($"������������ URL Traefik ��� ����������: {backendUrl}");
        }
        httpClient.BaseAddress = new Uri(backendUrl);
    }
    else
    {
        Console.WriteLine("����������� �����: ��������� HttpClient � ������������� BaseAddress.");
        httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    }
    return httpClient;
});

builder.Services.AddScoped<IExperienceApi, ExperienceApiService>();
builder.Services.AddScoped<IUrlHelperService, UrlHelperService>();
builder.Services.AddScoped<IUrlStateManager, UrlStateManager>();
builder.Services.AddScoped<ILocalizationService, LocalizationService>();

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.PropertyNameCaseInsensitive = true;
});

await builder.Build().RunAsync();