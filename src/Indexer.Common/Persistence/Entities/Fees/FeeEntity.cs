namespace Indexer.Common.Persistence.Entities.Fees
{
    internal sealed class FeeEntity
    {
        // ReSharper disable InconsistentNaming
        public string transaction_id { get; set; }
        public long asset_id { get; set; }
        public decimal amount { get; set; }
    }
}
