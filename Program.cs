using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WTExpCalc;
using WTExpCalc.Services;
using System.Net.Http;
using System.Text.Json; // Убедитесь, что using есть

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Регистрация HttpClient для отправки запросов на тот же origin,
// с которого загружено приложение. Traefik будет маршрутизировать /api/*
builder.Services.AddScoped(sp => { // Используем лямбду для доступа к builder внутри AddScoped
    var httpClient = new HttpClient();
    if (builder.HostEnvironment.IsDevelopment())
    {
        // Среда разработки (dotnet run)
        Console.WriteLine("Режим разработки: Настройка HttpClient для доступа через Traefik на сервере.");
        // Читаем АБСОЛЮТНЫЙ URL Traefik из конфигурации
        var backendUrl = builder.Configuration["BackendApi:BaseUrl"];
        if (string.IsNullOrEmpty(backendUrl))
        {
            backendUrl = "http://192.168.0.105:8881/"; // Дефолт, если не найден
            Console.WriteLine($"URL Traefik для разработки не найден, используется дефолтный: {backendUrl}");
        }
        else
        {
            Console.WriteLine($"Используется URL Traefik для разработки: {backendUrl}");
        }
        httpClient.BaseAddress = new Uri(backendUrl);
    }
    else
    {
        // Среда НЕ разработки (Production, в Docker)
        // Используем относительный базовый адрес (адрес самого приложения)
        Console.WriteLine("Режим НЕ разработки: Настройка HttpClient с относительным BaseAddress.");
        httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    }
    return httpClient; // Возвращаем сконфигурированный HttpClient
});
builder.Services.AddScoped<IExperienceApi, ExperienceApiService>();

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.PropertyNameCaseInsensitive = true; 
});


await builder.Build().RunAsync();