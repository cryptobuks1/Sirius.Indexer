using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions
{
    public sealed class TransactionHeader
    {
        public TransactionHeader(string id, 
            int number,
            TransactionBroadcastingError error)
        {
            Id = id;
            Number = number;
            Error = error;
        }

        public string Id { get; }
        public int Number { get; }
        public TransactionBroadcastingError Error { get; }
    }
}
