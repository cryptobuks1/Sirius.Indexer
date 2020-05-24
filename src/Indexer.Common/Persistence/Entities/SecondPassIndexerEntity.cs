namespace Indexer.Common.Persistence.Entities
{
    public class SecondPassIndexerEntity
    {
        public string BlockchainId { get; set; }
        public long NextBlock { get;  set; }
        public long StopBlock { get; set; }
        public int Version { get; set; }
    }
}
