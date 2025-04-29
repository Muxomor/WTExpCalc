using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WTExpCalc;
using WTExpCalc.Services;
using System.Net.Http;
using System.Text.Json; // ���������, ��� using ����

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ����������� HttpClient ��� �������� �������� �� ��� �� origin,
// � �������� ��������� ����������. Traefik ����� ���������������� /api/*
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// ����������� ������ ������� API
builder.Services.AddScoped<IExperienceApi, ExperienceApiService>();

// ��������� ����� JSON ������������ (���� ��� API ���������� snake_case)
// �������� ���� ����, ���� �� ����� ������ API (PostgREST ������ ���������� snake_case)
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.PropertyNameCaseInsensitive = true; 
});


await builder.Build().RunAsync();