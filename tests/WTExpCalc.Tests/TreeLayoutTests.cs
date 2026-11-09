using WTExpCalc.Models;
using WTExpCalc.Services;
using Xunit;

namespace WTExpCalc.Tests;

public class TreeLayoutTests
{
    [Fact]
    public void Build_SeparatesGridNodesAndFolderChildren()
    {
        var nodes = new List<Node>
        {
            Vehicle(1, rank: 1, column: 0, row: 0, requiredExp: 5000),
            Premium(2, rank: 1, column: 0, row: 0),
            Folder(3, rank: 2, column: 0, row: 0),
            Vehicle(4, rank: 2, column: 0, row: 0, requiredExp: 10000, orderInFolder: 0),
            Vehicle(5, rank: 2, column: 0, row: 0, requiredExp: 15000, orderInFolder: 1)
        };

        var requirements = new List<RankRequirement>
        {
            new() { PreviousRank = 1, TargetRank = 2, RequiredUnits = 2 }
        };

        var layout = TreeLayout.Build(nodes, requirements);

        Assert.Equal(new[] { 1, 2 }, layout.ActiveRanks);
        Assert.Equal(new[] { 1, 2, 3 }, layout.GridNodes.Select(node => node.Id));
        Assert.Equal(0, layout.RankStartRows[1]);
        Assert.Equal(1, layout.RankStartRows[2]);

        var folder = nodes.Single(node => node.Type == "folder");
        var children = layout.GetFolderChildren(folder);
        Assert.Equal(new[] { 4, 5 }, children.Select(node => node.Id));

        Assert.Contains("grid-column: 2;", layout.GetNodeGridStyle(folder));
        Assert.Contains("grid-row: 3;", layout.GetNodeGridStyle(folder));
        Assert.Contains("grid-column: 2;", layout.GetNodeGridStyle(nodes[0]));
        Assert.Contains("grid-column: 3;", layout.GetNodeGridStyle(nodes[1]));
    }

    [Fact]
    public void Build_WithoutNodes_ReturnsEmptyLayout()
    {
        var layout = TreeLayout.Build(Array.Empty<Node>(), Array.Empty<RankRequirement>());

        Assert.Empty(layout.GridNodes);
        Assert.Empty(layout.ActiveRanks);
        Assert.Empty(layout.RankStartRows);
        Assert.Empty(layout.GridStyles);
    }

    private static Node Vehicle(
        int id,
        int rank,
        int column,
        int row,
        int? requiredExp,
        int? orderInFolder = null)
        => new()
        {
            Id = id,
            ExternalId = $"vehicle_{id}",
            Name = $"Vehicle {id}",
            Type = "vehicle",
            TechCategory = "standard",
            Rank = rank,
            ColumnIndex = column,
            RowIndex = row,
            RequiredExp = requiredExp,
            OrderInFolder = orderInFolder
        };

    private static Node Premium(int id, int rank, int column, int row)
        => new()
        {
            Id = id,
            ExternalId = $"premium_{id}",
            Name = $"Premium {id}",
            Type = "vehicle",
            TechCategory = "premium",
            Rank = rank,
            ColumnIndex = column,
            RowIndex = row
        };

    private static Node Folder(int id, int rank, int column, int row)
        => new()
        {
            Id = id,
            ExternalId = $"folder_{id}",
            Name = $"Folder {id}",
            Type = "folder",
            TechCategory = "standard",
            Rank = rank,
            ColumnIndex = column,
            RowIndex = row
        };
}
