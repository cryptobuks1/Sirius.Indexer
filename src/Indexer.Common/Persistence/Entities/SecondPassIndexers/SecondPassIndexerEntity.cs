using System;

namespace Indexer.Common.Persistence.Entities.SecondPassIndexers
{
    public class SecondPassIndexerEntity
    {
        public string BlockchainId { get; set; }
        public long NextBlock { get;  set; }
        public long StopBlock { get; set; }
        public int Version { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
