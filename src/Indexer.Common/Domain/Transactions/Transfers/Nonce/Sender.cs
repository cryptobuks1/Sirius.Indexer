namespace Indexer.Common.Domain.Transactions.Transfers.Nonce
{
    public sealed class Sender
    {
        public Sender(string address)
        {
            Address = address;
        }

        public string Address { get; }
    }
}
