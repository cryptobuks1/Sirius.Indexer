using System;

namespace Indexer.Common.Persistence.Entities
{
    public class BlockHeaderEntity
    {
        public string GlobalId { get; set; }
        public string BlockchainId { get; set; }
        public string Id { get; set; }
        public long Number { get; set; }
        public string PreviousId { get; set; }
        public DateTime MinedAt { get; set; }
    }
}
