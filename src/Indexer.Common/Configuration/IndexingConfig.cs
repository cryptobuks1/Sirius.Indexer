using System.Collections.Generic;

namespace Indexer.Common.Configuration
{
    public class IndexingConfig
    {
        public IReadOnlyDictionary<string, BlockchainIndexingConfig> Blockchains { get; set; }
    }
}
