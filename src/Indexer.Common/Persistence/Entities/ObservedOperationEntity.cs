using System.ComponentModel.DataAnnotations;

namespace Indexer.Common.Persistence.Entities
{
    public class ObservedOperationEntity
    {
        public string BlockchainId { get; set; }

        [Key]
        public long OperationId { get; set; }

        public string TransactionId { get; set; }

        public bool IsCompleted { get; set; }
    }
}
