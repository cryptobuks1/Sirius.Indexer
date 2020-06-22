using System;

namespace Indexer.Common.Persistence.Entities.ObservedOperations
{
    internal sealed class ObservedOperationEntity
    {
        // ReSharper disable InconsistentNaming
        public long id { get; set; }
        public string transaction_id { get; set; }
        public DateTime added_at { get; set; }
    }
}
