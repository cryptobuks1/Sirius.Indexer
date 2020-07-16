namespace Indexer.Common.Domain.Transactions.Transfers.Nonce
{
    public sealed class NonceUpdate
    {
        public NonceUpdate(string address, string blockId, long nonce)
        {
            Address = address;
            BlockId = blockId;
            Nonce = nonce;
        }

        public string Address { get; }
        public string BlockId { get; }
        public long Nonce { get; }
    }
}
