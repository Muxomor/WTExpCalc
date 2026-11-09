using WTExpCalc.Models;

namespace WTExpCalc.Services.Tree
{
    public enum SelectionToggleResult
    {
        Added,
        Removed,
        AutoAdjusted,
        LimitExceeded
    }

    /// <summary>
    /// Чистое состояние выбора: выбранные id, RP-overrides и производные итоги.
    /// Не знает про Blazor, JS и URL.
    /// </summary>
    public sealed class TreeSelectionState
    {
        private readonly IReadOnlyDictionary<int, Node> _nodes;
        private readonly IReadOnlyDictionary<int, int> _originalRpValues;
        private readonly HashSet<int> _selectedIds = new();
        private readonly Dictionary<int, int> _rpOverrides = new();
        private List<Node> _selectedNodes = new();
        private List<int> _selectedRanks = new();

        public TreeSelectionState(
            IReadOnlyDictionary<int, Node> nodes,
            IReadOnlyDictionary<int, int> originalRpValues)
        {
            _nodes = nodes;
            _originalRpValues = originalRpValues;
            Recalculate();
        }

        public IReadOnlySet<int> SelectedIds => _selectedIds;
        public IReadOnlyDictionary<int, int> RpOverrides => _rpOverrides;
        public IReadOnlyList<Node> SelectedNodes => _selectedNodes;
        public IReadOnlyList<int> SelectedRanks => _selectedRanks;
        public long TotalRp { get; private set; }
        public long TotalSl { get; private set; }

        public bool IsSelected(Node node) => _selectedIds.Contains(node.Id);

        public int GetEffectiveRp(Node node)
        {
            return _rpOverrides.TryGetValue(node.Id, out var value)
                ? value
                : node.RequiredExp ?? 0;
        }

        public bool IsRpModified(Node node)
        {
            return TreeSummaryFormatter.IsRpValueModified(node, _originalRpValues, _rpOverrides);
        }

        public void Clear()
        {
            _selectedIds.Clear();
            _rpOverrides.Clear();
            Recalculate();
        }

        public void Restore(
            IEnumerable<int> selectedIds,
            IReadOnlyDictionary<int, int> rpOverrides)
        {
            _selectedIds.Clear();
            _rpOverrides.Clear();

            foreach (var id in selectedIds)
            {
                if (_nodes.TryGetValue(id, out var node) && node.Type == "vehicle")
                {
                    _selectedIds.Add(id);
                }
            }

            foreach (var pair in rpOverrides)
            {
                if (_nodes.TryGetValue(pair.Key, out var node) && node.RequiredExp.HasValue)
                {
                    _rpOverrides[pair.Key] = pair.Value;
                }
            }

            Recalculate();
        }

        public SelectionToggleResult Toggle(Node node, long? maxRpLimit)
        {
            if (node.Type != "vehicle")
            {
                return SelectionToggleResult.Removed;
            }

            if (_selectedIds.Contains(node.Id))
            {
                _selectedIds.Remove(node.Id);
                _rpOverrides.Remove(node.Id);
                Recalculate();
                return SelectionToggleResult.Removed;
            }

            long nodeRp = GetEffectiveRp(node);

            if (!maxRpLimit.HasValue)
            {
                _selectedIds.Add(node.Id);
                Recalculate();
                return SelectionToggleResult.Added;
            }

            long remaining = maxRpLimit.Value - TotalRp;

            if (nodeRp <= remaining)
            {
                _selectedIds.Add(node.Id);
                Recalculate();
                return SelectionToggleResult.Added;
            }

            if (remaining > 0 && nodeRp > 0)
            {
                _rpOverrides[node.Id] = (int)remaining;
                _selectedIds.Add(node.Id);
                Recalculate();
                return SelectionToggleResult.AutoAdjusted;
            }

            return SelectionToggleResult.LimitExceeded;
        }

        public bool RemoveExcess(long limit)
        {
            if (TotalRp <= limit)
            {
                return false;
            }

            var selectedNodesList = _selectedNodes
                .Where(node => node.RequiredExp.HasValue)
                .OrderByDescending(node => node.Rank)
                .ThenByDescending(node => node.ColumnIndex ?? 0)
                .ToList();

            long currentTotal = TotalRp;
            var removed = false;

            foreach (var node in selectedNodesList)
            {
                if (currentTotal <= limit)
                {
                    break;
                }

                currentTotal -= GetEffectiveRp(node);
                _selectedIds.Remove(node.Id);
                _rpOverrides.Remove(node.Id);
                removed = true;
            }

            if (removed)
            {
                Recalculate();
            }

            return removed;
        }

        public void SetRpOverride(Node node, int value)
        {
            if (!node.RequiredExp.HasValue)
            {
                return;
            }

            value = Math.Max(0, value);

            if (_originalRpValues.TryGetValue(node.Id, out var original) && original == value)
            {
                _rpOverrides.Remove(node.Id);
            }
            else
            {
                _rpOverrides[node.Id] = value;
            }

            Recalculate();
        }

        private void Recalculate()
        {
            _selectedNodes = _selectedIds
                .Select(id => _nodes.TryGetValue(id, out var node) ? node : null)
                .OfType<Node>()
                .Where(node => node.Type == "vehicle")
                .ToList();

            TotalRp = 0;
            TotalSl = 0;

            foreach (var node in _selectedNodes.Where(node => node.RequiredExp.HasValue))
            {
                TotalRp += GetEffectiveRp(node);
                TotalSl += node.SilverCost ?? 0;
            }

            _selectedRanks = _selectedNodes
                .Select(node => node.Rank)
                .Distinct()
                .OrderBy(rank => rank)
                .ToList();
        }
    }
}
