using WTExpCalc.Models;

namespace WTExpCalc.Services
{
    /// <summary>
    /// Предвычисленная геометрия дерева. Все методы ниже чистые и не зависят от Blazor/JS.
    /// </summary>
    public sealed class TreeLayout
    {
        public const int NodeCardWidth = 204;
        public const int NodeCardHeight = 87;

        public static readonly TreeLayout Empty = new(
            Array.Empty<Node>(),
            Array.Empty<int>(),
            new Dictionary<int, int>(),
            new Dictionary<int, int>(),
            new Dictionary<int, RankRequirement>(),
            new Dictionary<int, IReadOnlyList<Node>>(),
            new Dictionary<int, IReadOnlyList<Node>>(),
            0,
            0,
            0,
            0,
            string.Empty);

        private TreeLayout(
            IReadOnlyList<Node> gridNodes,
            IReadOnlyList<int> activeRanks,
            IReadOnlyDictionary<int, int> maxRowIndexPerRank,
            IReadOnlyDictionary<int, int> rankStartRows,
            IReadOnlyDictionary<int, RankRequirement> rankRequirementsByPrevious,
            IReadOnlyDictionary<int, IReadOnlyList<Node>> nodesByRank,
            IReadOnlyDictionary<int, IReadOnlyList<Node>> folderChildren,
            int totalDataGridRows,
            int standardColumnCount,
            int premiumColumnCount,
            int maxRank,
            string gridStyles)
        {
            GridNodes = gridNodes;
            ActiveRanks = activeRanks;
            MaxRowIndexPerRank = maxRowIndexPerRank;
            RankStartRows = rankStartRows;
            RankRequirementsByPrevious = rankRequirementsByPrevious;
            NodesByRank = nodesByRank;
            FolderChildren = folderChildren;
            TotalDataGridRows = totalDataGridRows;
            StandardColumnCount = standardColumnCount;
            PremiumColumnCount = premiumColumnCount;
            MaxRank = maxRank;
            GridStyles = gridStyles;
        }

        public IReadOnlyList<Node> GridNodes { get; }
        public IReadOnlyList<int> ActiveRanks { get; }
        public IReadOnlyDictionary<int, int> MaxRowIndexPerRank { get; }
        public IReadOnlyDictionary<int, int> RankStartRows { get; }
        public IReadOnlyDictionary<int, RankRequirement> RankRequirementsByPrevious { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<Node>> NodesByRank { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<Node>> FolderChildren { get; }
        public int TotalDataGridRows { get; }
        public int StandardColumnCount { get; }
        public int PremiumColumnCount { get; }
        public int MaxRank { get; }
        public string GridStyles { get; }

        public int PremiumDividerColumn => 2 + StandardColumnCount;
        public int LastDataRow => 1 + TotalDataGridRows;

        public static TreeLayout Build(
            IReadOnlyList<Node> nodes,
            IReadOnlyList<RankRequirement> rankRequirements)
        {
            if (nodes.Count == 0)
            {
                return Empty;
            }

            var maxRowIndexPerRank = new Dictionary<int, int>();
            var nodesByRank = new Dictionary<int, List<Node>>();
            var folderNodes = new List<Node>();
            var gridNodes = new List<Node>();
            var childrenByKey = new Dictionary<FolderKey, List<Node>>();

            foreach (var node in nodes)
            {
                if (!nodesByRank.TryGetValue(node.Rank, out var rankNodes))
                {
                    rankNodes = new List<Node>();
                    nodesByRank[node.Rank] = rankNodes;
                }

                rankNodes.Add(node);

                int currentMax = maxRowIndexPerRank.TryGetValue(node.Rank, out var max) ? max : -1;
                if ((node.RowIndex ?? 0) > currentMax)
                {
                    maxRowIndexPerRank[node.Rank] = node.RowIndex ?? 0;
                }
                else if (!maxRowIndexPerRank.ContainsKey(node.Rank))
                {
                    maxRowIndexPerRank[node.Rank] = node.RowIndex ?? 0;
                }

                if (node.Type == "folder")
                {
                    folderNodes.Add(node);
                    gridNodes.Add(node);
                }
                else if (node.Type == "vehicle" && !node.OrderInFolder.HasValue)
                {
                    gridNodes.Add(node);
                }

                if (node.OrderInFolder.HasValue)
                {
                    var key = FolderKey.From(node);
                    if (!childrenByKey.TryGetValue(key, out var children))
                    {
                        children = new List<Node>();
                        childrenByKey[key] = children;
                    }

                    children.Add(node);
                }
            }

            var folderChildren = new Dictionary<int, IReadOnlyList<Node>>();
            foreach (var folder in folderNodes)
            {
                if (childrenByKey.TryGetValue(FolderKey.From(folder), out var children))
                {
                    folderChildren[folder.Id] = children
                        .OrderBy(node => node.OrderInFolder!.Value)
                        .ToList();
                }
                else
                {
                    folderChildren[folder.Id] = Array.Empty<Node>();
                }
            }

            var activeRanks = rankRequirements
                .Select(requirement => requirement.PreviousRank)
                .Concat(nodes.Select(node => node.Rank))
                .Distinct()
                .OrderBy(rank => rank)
                .ToList();

            var rankStartRows = new Dictionary<int, int>();
            int cumulativeRows = 0;
            foreach (var rank in activeRanks)
            {
                rankStartRows[rank] = cumulativeRows;
                cumulativeRows += (maxRowIndexPerRank.TryGetValue(rank, out var maxRow) ? maxRow : 0) + 1;
            }

            var rankRequirementsByPrevious = rankRequirements
                .GroupBy(requirement => requirement.PreviousRank)
                .ToDictionary(group => group.Key, group => group.First());

            var standardColumnCount = nodes
                .Where(node => node.TechCategory == "standard")
                .Select(node => (node.ColumnIndex ?? 0) + 1)
                .DefaultIfEmpty(0)
                .Max();

            var premiumColumnCount = nodes
                .Where(node => node.TechCategory != "standard")
                .Select(node => (node.ColumnIndex ?? 0) + 1)
                .DefaultIfEmpty(0)
                .Max();

            int maxRank = nodes.Max(node => node.Rank);
            int totalDataGridRows = maxRowIndexPerRank.Sum(pair => pair.Value + 1);

            string gridRowsStyle = "auto";
            if (totalDataGridRows > 0)
            {
                gridRowsStyle += $" repeat({totalDataGridRows}, {NodeCardHeight}px)";
            }
            else if (maxRank > 0)
            {
                gridRowsStyle += $" repeat({maxRank}, {NodeCardHeight}px)";
            }

            string gridStyles =
                $"display: grid; " +
                $"grid-template-rows: {gridRowsStyle}; " +
                $"grid-template-columns: 60px {(standardColumnCount > 0 ? $"repeat({standardColumnCount}, {NodeCardWidth}px) " : "")}{(premiumColumnCount > 0 ? $"repeat({premiumColumnCount}, {NodeCardWidth}px)" : "")}; " +
                $"gap: 30px 40px; position: relative;";

            return new TreeLayout(
                gridNodes,
                activeRanks,
                maxRowIndexPerRank,
                rankStartRows,
                rankRequirementsByPrevious,
                nodesByRank.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Node>)pair.Value),
                folderChildren,
                totalDataGridRows,
                standardColumnCount,
                premiumColumnCount,
                maxRank,
                gridStyles);
        }

        public IReadOnlyList<Node> GetFolderChildren(Node folder)
        {
            return FolderChildren.TryGetValue(folder.Id, out var children)
                ? children
                : Array.Empty<Node>();
        }

        public string GetNodeGridStyle(Node node)
        {
            int column = node.TechCategory == "standard"
                ? 2 + (node.ColumnIndex ?? 0)
                : 2 + StandardColumnCount + (node.ColumnIndex ?? 0);

            int startRow = 2
                + (RankStartRows.TryGetValue(node.Rank, out var rankStart) ? rankStart : 0)
                + (node.RowIndex ?? 0);

            return $"grid-column: {column}; grid-row: {startRow}; z-index:1; position:relative;";
        }

        private readonly record struct FolderKey(
            int Rank,
            int? ColumnIndex,
            int? RowIndex,
            string TechCategory)
        {
            public static FolderKey From(Node node)
                => new(node.Rank, node.ColumnIndex, node.RowIndex, node.TechCategory);
        }
    }
}
