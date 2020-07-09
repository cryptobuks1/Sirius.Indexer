namespace Indexer.Common.Persistence.Entities.NonceUpdates
{
    internal sealed class NonceUpdateEntity
    {
        // ReSharper disable InconsistentNaming
        public string address { get; set; }
        public string transaction_id { get; set; }
        public long nonce { get; set; }
    }
}
