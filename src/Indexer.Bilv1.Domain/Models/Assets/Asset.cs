namespace Indexer.Bilv1.Domain.Models.Assets
{
    public sealed class Asset
    {
        // TODO: Use guid for ID?
        public string AssetId { get; set; }
        public string Ticker { get; set; }
        public string Address { get; set; }
        public int Accuracy { get;set; }
    }
}
