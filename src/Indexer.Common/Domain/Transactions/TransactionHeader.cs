using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions
{
    public sealed class TransactionHeader
    {
        public TransactionHeader(
            string blockchainId,
            string blockId,
            string id, 
            int number,
            TransactionBroadcastingError error)
        {
            GlobalId = $"{blockchainId}-{id}";
            BlockchainId = blockchainId;
            BlockId = blockId;
            Id = id;
            Number = number;
            Error = error;
        }

        public string GlobalId { get; }
        public string BlockchainId { get; }
        public string BlockId { get; }
        public string Id { get; }
        public int Number { get; }
        public TransactionBroadcastingError Error { get; }

        public override string ToString()
        {
            return GlobalId;
        }
    }
}
