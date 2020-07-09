namespace Indexer.Common.Domain.Transactions.Transfers.Nonce
{
    public sealed class NonceUpdate
    {
        public NonceUpdate(string address, long nonce)
        {
            Address = address;
            Nonce = nonce;
        }

        public string Address { get; }
        public long Nonce { get; }
    }
}
