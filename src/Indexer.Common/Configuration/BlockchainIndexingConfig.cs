using System;

namespace Indexer.Common.Configuration
{
    public class BlockchainIndexingConfig
    {
        public int FirstPassIndexersCount { get; set; }
        public long LastHistoricalBlockNumber { get; set; }
        public TimeSpan DelayOnBlockNotFound { get; set; }
    }
}
