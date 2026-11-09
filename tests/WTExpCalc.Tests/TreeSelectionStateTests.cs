using WTExpCalc.Models;
using WTExpCalc.Services.Tree;
using Xunit;

namespace WTExpCalc.Tests;

public class TreeSelectionStateTests
{
    [Fact]
    public void Toggle_AddsAndRemovesNodesAndKeepsTotals()
    {
        var (state, nodes) = CreateState();

        var first = state.Toggle(nodes[0], maxRpLimit: null);
        var second = state.Toggle(nodes[2], maxRpLimit: null);

        Assert.Equal(SelectionToggleResult.Added, first);
        Assert.Equal(SelectionToggleResult.Added, second);
        Assert.Equal(new HashSet<int> { 1, 3 }, state.SelectedIds);
        Assert.Equal(7900, state.TotalRp);

        var removed = state.Toggle(nodes[0], maxRpLimit: null);

        Assert.Equal(SelectionToggleResult.Removed, removed);
        Assert.Equal(new HashSet<int> { 3 }, state.SelectedIds);
        Assert.Equal(0, state.TotalRp);
    }

    [Fact]
    public void Toggle_WithLimit_AutoAdjustsThenRejectsExcess()
    {
        var (state, nodes) = CreateState();

        var adjusted = state.Toggle(nodes[0], maxRpLimit: 5000);
        var rejected = state.Toggle(nodes[1], maxRpLimit: 5000);

        Assert.Equal(SelectionToggleResult.AutoAdjusted, adjusted);
        Assert.Equal(SelectionToggleResult.LimitExceeded, rejected);
        Assert.Equal(new HashSet<int> { 1 }, state.SelectedIds);
        Assert.Equal(5000, state.TotalRp);
        Assert.Equal(5000, state.GetEffectiveRp(nodes[0]));
        Assert.True(state.IsRpModified(nodes[0]));
    }

    [Fact]
    public void RemoveExcess_RemovesHighestRanksUntilLimit()
    {
        var (state, nodes) = CreateState();
        state.Toggle(nodes[0], maxRpLimit: null); // rank 1, 7900
        state.Toggle(nodes[1], maxRpLimit: null); // rank 2, 7900
        state.Toggle(nodes[2], maxRpLimit: null); // rank 2, 1000

        var removed = state.RemoveExcess(9000);

        Assert.True(removed);
        Assert.True(state.TotalRp <= 9000);
        Assert.Contains(1, state.SelectedIds);
        Assert.DoesNotContain(2, state.SelectedIds);
    }

    [Fact]
    public void Restore_IgnoresUnknownIdsAndRpForPremiumNodes()
    {
        var (state, _) = CreateState();

        state.Restore(
            selectedIds: new[] { 1, 99, 3 },
            rpOverrides: new Dictionary<int, int> { [1] = 5000, [3] = 1000 });

        Assert.Equal(new HashSet<int> { 1, 3 }, state.SelectedIds);
        Assert.Equal(5000, state.GetEffectiveRp(new Node { Id = 1, RequiredExp = 7900 }));
        Assert.False(state.RpOverrides.ContainsKey(3));
    }

    [Fact]
    public void SetRpOverride_RemovesOverrideWhenValueReturnsToOriginal()
    {
        var (state, nodes) = CreateState();

        state.Toggle(nodes[1], maxRpLimit: null);
        state.SetRpOverride(nodes[1], 5000);
        Assert.True(state.IsRpModified(nodes[1]));

        state.SetRpOverride(nodes[1], 7900);
        Assert.False(state.IsRpModified(nodes[1]));
        Assert.Empty(state.RpOverrides);
    }

    private static (TreeSelectionState State, List<Node> Nodes) CreateState()
    {
        var nodes = new List<Node>
        {
            new() { Id = 1, ExternalId = "rank1", Type = "vehicle", TechCategory = "standard", Rank = 1, ColumnIndex = 0, RequiredExp = 7900 },
            new() { Id = 2, ExternalId = "rank2", Type = "vehicle", TechCategory = "standard", Rank = 2, ColumnIndex = 0, RequiredExp = 7900 },
            new() { Id = 3, ExternalId = "premium", Type = "vehicle", TechCategory = "premium", Rank = 1, ColumnIndex = 0 }
        };

        var originals = nodes
            .Where(node => node.RequiredExp.HasValue)
            .ToDictionary(node => node.Id, node => node.RequiredExp!.Value);

        return (new TreeSelectionState(nodes.ToDictionary(node => node.Id), originals), nodes);
    }
}
