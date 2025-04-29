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
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Регистрация вашего сервиса API
builder.Services.AddScoped<IExperienceApi, ExperienceApiService>();

// Настройка опций JSON сериализации (если ваш API использует snake_case)
// Оставьте этот блок, если он нужен вашему API (PostgREST обычно использует snake_case)
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.PropertyNameCaseInsensitive = true; 
});


await builder.Build().RunAsync();