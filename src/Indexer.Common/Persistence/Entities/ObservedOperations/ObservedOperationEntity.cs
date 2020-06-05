using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.ObservedOperations
{
    public class ObservedOperationEntity
    {
        public string BlockchainId { get; set; }

        [Key]
        public long OperationId { get; set; }

        public string TransactionId { get; set; }

        public bool IsCompleted { get; set; }

        public Guid BilV1OperationId { get; set; }

        public long AssetId { get; set; }

        public IReadOnlyCollection<Unit> Fees { get; set; }

        public string DestinationAddress { get; set; }

        public decimal OperationAmount { get; set; }

    }
}
