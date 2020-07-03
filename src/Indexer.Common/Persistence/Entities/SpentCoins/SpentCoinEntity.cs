namespace Indexer.Common.Persistence.Entities.SpentCoins
{
    internal sealed class SpentCoinEntity
    {
        // ReSharper disable InconsistentNaming
        public string transaction_id { get; set; }
        public int number { get; set; }
        public long asset_id { get; set; }
        public decimal amount { get; set; }
        public string address { get; set; }
        public string script_pub_key { get; set; }
        public string tag { get; set; }
        public int? tag_type { get; set; }
        public string spent_by_transaction_id { get; set; }
        public int spent_by_input_coin_number { get; set; }
    }
}
