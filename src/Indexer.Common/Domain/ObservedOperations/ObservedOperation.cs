
namespace Indexer.Common.Domain.ObservedOperations
{
    public class ObservedOperation
    {
        private ObservedOperation(
            long operationId, 
            string blockchainId, 
            string transactionId,
            bool isCompleted)
        {
            OperationId = operationId;
            BlockchainId = blockchainId;
            TransactionId = transactionId;
            IsCompleted = isCompleted;
        }

        public string BlockchainId { get; }
        
        public long OperationId { get; }

        public string TransactionId { get; }

        public bool IsCompleted { get; }

        public static ObservedOperation Create(
            long operationId,
            string blockchainId,
            string transactionId)
        {
            return new ObservedOperation(
                operationId,
                blockchainId,
                transactionId,
                false);
        }
    }
}
