namespace Indexer.Common.Configuration
{
    public class BlockchainConfig
    {
        public BlockchainDbConfig Db { get; set; }
        public BlockchainIndexingConfig Indexing { get; set; }
    }
}
