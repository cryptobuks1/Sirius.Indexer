using System;

namespace Indexer.Common.Persistence.Entities
{
    public class OngoingIndexerEntity
    {
        public string BlockchainId { get; set; }
        public long StartBlock { get; set; }
        public long NextBlock { get; set; }
        public long Sequence { get; set; }
        public int Version { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
