using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WTExpCalc;
using WTExpCalc.Services;
using System.Net.Http;
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
        Console.WriteLine("Стандартный режим: Настройка HttpClient с относительным BaseAddress.");
        httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    }
    return httpClient;
});

builder.Services.AddScoped<IExperienceApi, ExperienceApiService>();
builder.Services.AddScoped<IUrlHelperService, UrlHelperService>();

builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.PropertyNameCaseInsensitive = true;
});

await builder.Build().RunAsync();