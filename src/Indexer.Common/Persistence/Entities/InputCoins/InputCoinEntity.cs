namespace Indexer.Common.Persistence.Entities.InputCoins
{
    internal sealed class InputCoinEntity
    {
        // ReSharper disable InconsistentNaming
        public string block_id { get; set; }
        public string transaction_id { get; set; }
        public int number { get; set; }
        public int type { get; set; }
        public string prev_output_transaction_id { get; set; }
        public int? prev_output_coin_number { get; set; }
    }
}
