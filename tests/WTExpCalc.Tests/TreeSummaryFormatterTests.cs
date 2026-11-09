using WTExpCalc.Models;
using WTExpCalc.Services;
using WTExpCalc.Shared;
using Xunit;

namespace WTExpCalc.Tests;

public class TreeSummaryFormatterTests
{
    [Fact]
    public void Format_UsesEffectiveRpAndShowsOriginalValue()
    {
        var formatter = new TreeSummaryFormatter(new TestLocalizationService());
        var vehicle = new Node
        {
            Id = 1,
            Name = "P-400",
            Type = "vehicle",
            TechCategory = "standard",
            Rank = 2,
            ColumnIndex = 0,
            RequiredExp = 7900
        };
        var premium = new Node
        {
            Id = 2,
            Name = "Premium",
            Type = "vehicle",
            TechCategory = "premium",
            Rank = 2,
            ColumnIndex = 0
        };

        var text = formatter.Format(
            new[] { vehicle, premium },
            new Dictionary<int, int> { [vehicle.Id] = 7900 },
            new Dictionary<int, int> { [vehicle.Id] = 5000 },
            totalRp: 5000);

        Assert.Contains("P-400 (5000 / 7900)", text);
        Assert.Contains("Premium", text);
        Assert.Contains("Сумма опыта", text);
    }

    [Fact]
    public void IsRpValueModified_ReturnsFalseWhenOverrideMatchesOriginal()
    {
        var vehicle = new Node
        {
            Id = 1,
            Type = "vehicle",
            TechCategory = "standard",
            RequiredExp = 10000
        };

        Assert.False(TreeSummaryFormatter.IsRpValueModified(
            vehicle,
            new Dictionary<int, int> { [1] = 10000 },
            new Dictionary<int, int>()));
        Assert.True(TreeSummaryFormatter.IsRpValueModified(
            vehicle,
            new Dictionary<int, int> { [1] = 10000 },
            new Dictionary<int, int> { [1] = 5000 }));
        Assert.False(TreeSummaryFormatter.IsRpValueModified(
            vehicle,
            new Dictionary<int, int> { [1] = 10000 },
            new Dictionary<int, int> { [1] = 10000 }));
    }

    private sealed class TestLocalizationService : ILocalizationService
    {
        public string CurrentLanguage => "ru";
        public bool IsEnglish => false;
        public event Action? LanguageChanged { add { } remove { } }
        public string Text(LocalizedString text) => text.Ru;
        public string Text(LocalizedString text, params object[] args) => string.Format(text.Ru, args);
        public string NationName(string name, int? id = null) => name;
        public string VehicleTypeName(string russianName, string? englishName) => russianName;
        public string NodeName(string russianName, string? englishName) => russianName;
        public Task SetLanguageAsync(string language) => Task.CompletedTask;
        public Task<string> LoadSavedLanguageAsync() => Task.FromResult("ru");
    }
}
