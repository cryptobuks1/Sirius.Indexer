namespace Indexer.Common.Persistence.Entities.InputCoins
{
    internal sealed class InputCoinEntity
    {
        // ReSharper disable InconsistentNaming
        public string transaction_id { get; set; }
        public int number { get; set; }
        public string block_id { get; set; }
    }
}
