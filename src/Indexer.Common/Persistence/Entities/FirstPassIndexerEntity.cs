using System;

namespace Indexer.Common.Persistence.Entities
{
    public class FirstPassIndexerEntity
    {
        public string Id { get; set; }
        public string BlockchainId { get; set; }
        public long StartBlock { get; set; }
        public long StopBlock { get; set; }
        public long NextBlock { get; set; }
        public long StepSize { get; set; }
        public int Version { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
