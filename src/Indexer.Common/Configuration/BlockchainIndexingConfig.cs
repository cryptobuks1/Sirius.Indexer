using System;

namespace Indexer.Common.Configuration
{
    public class BlockchainIndexingConfig
    {
        public int FirstPassHistoryIndexersCount { get; set; }
        public long LastHistoricalBlockNumber { get; set; }
        public TimeSpan DelayOnBlockNotFound { get; set; }
    }
}
