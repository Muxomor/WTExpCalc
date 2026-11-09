namespace WTExpCalc.Models
{
    public sealed class TotalsUpdateEventArgs : EventArgs
    {
        public long TotalRp { get; set; }
        public long TotalSl { get; set; }
        public string SelectedNames { get; set; } = string.Empty;
        public string FormattedText { get; set; } = string.Empty;
    }
}
