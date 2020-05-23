namespace Indexer.Common.Persistence.Entities
{
    public class FirstPassIndexerEntity
    {
        public string BlockchainId { get; set; }
        public long StartBlock { get; set; }
        public long StopBlock { get; set; }
        public long NextBlock { get; set; }
        public int Version { get; set; }
    }
}
