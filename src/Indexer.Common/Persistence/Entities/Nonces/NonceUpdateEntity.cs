namespace Indexer.Common.Persistence.Entities.Nonces
{
    internal sealed class NonceUpdateEntity
    {
        // ReSharper disable InconsistentNaming
        public string address { get; set; }
        public string transaction_id { get; set; }
        public long value { get; set; }
    }
}
