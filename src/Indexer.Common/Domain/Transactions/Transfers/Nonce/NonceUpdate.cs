namespace Indexer.Common.Domain.Transactions.Transfers.Nonce
{
    public sealed class NonceUpdate
    {
        public NonceUpdate(string address, string transactionId, long nonce)
        {
            Address = address;
            TransactionId = transactionId;
            Nonce = nonce;
        }

        public string Address { get; }
        public string TransactionId { get; }
        public long Nonce { get; }
    }
}
