using System;

namespace Indexer.Common.Domain.ObservedOperations
{
    public sealed class ObservedOperation
    {
        private ObservedOperation(long id, 
            string blockchainId, 
            string transactionId,
            DateTime addedAt)
        {
            Id = id;
            BlockchainId = blockchainId;
            TransactionId = transactionId;
            AddedAt = addedAt;
        }

        public string BlockchainId { get; }
        public long Id { get; }
        public string TransactionId { get; }
        public DateTime AddedAt { get; }

        public static ObservedOperation Create(long id,
            string blockchainId,
            string transactionId)
        {
            return new ObservedOperation(
                id,
                blockchainId,
                transactionId,
                DateTime.UtcNow);
        }

        public static ObservedOperation Restore(long id,
            string blockchainId,
            string transactionId,
            DateTime addedAt)
        {
            return new ObservedOperation(
                id,
                blockchainId,
                transactionId,
                addedAt);
        }
    }
}
