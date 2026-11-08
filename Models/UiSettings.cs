using System.Text.Json.Serialization;

namespace WTExpCalc.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UiAlignment
    {
        Left,
        Center,
        Right
    }

    public sealed record UiSettings
    {
        public UiAlignment TreeAlignment { get; init; } = UiAlignment.Center;
        public bool ShowAboutOnStartup { get; init; } = true;
        public bool ShowSummaryOnScreenshot { get; init; } = true;
    }
}
