namespace Indexer.Common.Persistence.Entities.UnspentCoins
{
    internal sealed class UnspentCoinEntity
    {
        // ReSharper disable InconsistentNaming
        public string transaction_id { get; set; }
        public int number { get; set; }
        public long asset_id { get; set; }
        public decimal amount { get; set; }
        public string address { get; set; }
        public string tag { get; set; }
        public int? tag_type { get; set; }
    }
}
