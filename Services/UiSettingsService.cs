using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    public interface IUiSettingsService
    {
        UiSettings Current { get; }
        Task LoadAsync();
        Task UpdateAsync(UiSettings settings);
        Task ResetAsync();
        event Action? Changed;
    }

    public class UiSettingsService : IUiSettingsService
    {
        private const string StorageKey = "uiSettings";

        private readonly IJSRuntime _jsRuntime;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public UiSettingsService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public UiSettings Current { get; private set; } = new();

        public event Action? Changed;

        public async Task LoadAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var loaded = JsonSerializer.Deserialize<UiSettings>(json, _jsonOptions);
                    if (loaded != null)
                    {
                        Current = Normalize(loaded);
                    }
                }
            }
            catch
            {
                Current = new UiSettings();
            }
        }

        public async Task UpdateAsync(UiSettings settings)
        {
            Current = Normalize(settings);
            await PersistAsync();
            Changed?.Invoke();
        }

        public async Task ResetAsync()
        {
            Current = new UiSettings();
            await PersistAsync();
            Changed?.Invoke();
        }

        private async Task PersistAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(Current, _jsonOptions);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
            }
            catch
            {
                /* localStorage may be unavailable in some contexts */
            }
        }

        private static UiSettings Normalize(UiSettings settings)
        {
            static UiAlignment Valid(UiAlignment value)
                => Enum.IsDefined(value) ? value : UiAlignment.Center;

            return settings with
            {
                TreeAlignment = Valid(settings.TreeAlignment)
            };
        }
    }
}
