using Microsoft.AspNetCore.Components;
using WTExpCalc.Models;
using WTExpCalc.Services;
using Xunit;

namespace WTExpCalc.Tests;

public class UrlStateManagerTests
{
    [Fact]
    public void RestoreSelectionFromUrl_RestoresSelectedNodesAndRpOverrides()
    {
        var manager = new UrlStateManager(new TestNavigationManager(), null!);
        var nodes = new List<Node>
        {
            new() { Id = 1, ExternalId = "p-400", Type = "vehicle", RequiredExp = 7900 },
            new() { Id = 2, ExternalId = "premium", Type = "vehicle", RequiredExp = null },
            new() { Id = 3, ExternalId = "not-selected", Type = "vehicle", RequiredExp = 1000 }
        };

        var state = manager.RestoreSelectionFromUrl(
            nodes,
            "https://example.test/tree/usa/aircraft?selected=p-400,premium&rp=p-400:5000");

        Assert.Equal(new HashSet<int> { 1, 2 }, state.SelectedNodeIds);
        Assert.Equal(5000, state.ModifiedRpValues[1]);
        Assert.False(state.ModifiedRpValues.ContainsKey(2));
    }

    [Fact]
    public void UpdateUrlWithSelection_IncludesPremiumAndOnlyModifiedRp()
    {
        var navigation = new TestNavigationManager();
        var manager = new UrlStateManager(navigation, null!);
        var nodes = new List<Node>
        {
            new() { Id = 1, ExternalId = "p-400", Type = "vehicle", TechCategory = "standard", RequiredExp = 7900 },
            new() { Id = 2, ExternalId = "premium", Type = "vehicle", TechCategory = "premium", RequiredExp = null },
            new() { Id = 3, ExternalId = "unchanged", Type = "vehicle", TechCategory = "standard", RequiredExp = 1000 }
        };

        manager.UpdateUrlWithSelection(
            nodes,
            new HashSet<int> { 1, 2, 3 },
            new Dictionary<int, int> { [1] = 7900, [3] = 1000 },
            new Dictionary<int, int> { [1] = 5000 },
            includePremiumInShareUrl: true);

        Assert.NotNull(navigation.LastNavigation);
        Assert.Contains("selected=p-400,premium,unchanged", navigation.LastNavigation);
        Assert.Contains("rp=p-400:5000", navigation.LastNavigation);
        Assert.DoesNotContain("unchanged:", navigation.LastNavigation);
    }

    [Fact]
    public void UpdateUrlWithSelection_ExcludesPremiumWhenSettingIsOff()
    {
        var navigation = new TestNavigationManager();
        var manager = new UrlStateManager(navigation, null!);
        var nodes = new List<Node>
        {
            new() { Id = 1, ExternalId = "standard", Type = "vehicle", TechCategory = "standard", RequiredExp = 1000 },
            new() { Id = 2, ExternalId = "premium", Type = "vehicle", TechCategory = "premium", RequiredExp = null }
        };

        manager.UpdateUrlWithSelection(
            nodes,
            new HashSet<int> { 1, 2 },
            new Dictionary<int, int> { [1] = 1000 },
            new Dictionary<int, int>(),
            includePremiumInShareUrl: false);

        Assert.NotNull(navigation.LastNavigation);
        Assert.Contains("selected=standard", navigation.LastNavigation);
        Assert.DoesNotContain("premium", navigation.LastNavigation);
    }

    [Fact]
    public void RestoreSelectionFromUrl_IgnoresUnknownIdsAndMalformedRp()
    {
        var manager = new UrlStateManager(new TestNavigationManager(), null!);
        var nodes = new List<Node>
        {
            new() { Id = 1, ExternalId = "p-400", Type = "vehicle", RequiredExp = 7900 }
        };

        var state = manager.RestoreSelectionFromUrl(
            nodes,
            "https://example.test/tree/usa/aircraft?selected=missing,p-400&rp=p-400:not-a-number");

        Assert.Equal(new HashSet<int> { 1 }, state.SelectedNodeIds);
        Assert.Empty(state.ModifiedRpValues);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://example.test/", "https://example.test/");
        }

        public string? LastNavigation { get; private set; }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            LastNavigation = uri;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            LastNavigation = uri;
        }
    }
}
