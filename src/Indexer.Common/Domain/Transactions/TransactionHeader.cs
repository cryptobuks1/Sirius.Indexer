using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions
{
    public sealed class TransactionHeader
    {
        private TransactionHeader(
            long key,
            string blockchainId,
            string blockId,
            string id, 
            int number,
            TransactionBroadcastingError error)
        {
            BlockchainId = blockchainId;
            BlockId = blockId;
            Id = id;
            Number = number;
            Error = error;
            Key = key;
        }

        public long Key { get; }
        public string BlockchainId { get; }
        public string BlockId { get; }
        public string Id { get; }
        public int Number { get; }
        public TransactionBroadcastingError Error { get; }

        public static TransactionHeader Create(string blockchainId,
            string blockId,
            string id,
            int number,
            TransactionBroadcastingError error)
        {
            return new TransactionHeader(
                key: default,
                blockchainId,
                blockId,
                id,
                number,
                error);
        }

        public static TransactionHeader Restore(long key,
            string blockchainId,
            string blockId,
            string id,
            int number,
            TransactionBroadcastingError error)
        {
            return new TransactionHeader(
                key,
                blockchainId,
                blockId,
                id,
                number,
                error);
        }
        
        public override string ToString()
        {
            return $"{BlockchainId}:{Key}({Id})";
        }
    }
}
