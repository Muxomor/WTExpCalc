using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WTExpCalc;
using WTExpCalc.Services;
using System.Net.Http;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// читаем URL из appsettings.json (или дефолт)
var postgrestUrl = builder.Configuration["Postgrest:Url"]
                  ?? "http://localhost:3000";

// регистрируем HttpClient c базовым адресом PostgREST
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(postgrestUrl)
});

// наш сервис, принимает HttpClient через ctor
builder.Services.AddScoped<IExperienceApi, ExperienceApiService>();
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.PropertyNameCaseInsensitive = true;
});

await builder.Build().RunAsync();
