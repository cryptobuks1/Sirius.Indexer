namespace Indexer.Common.Domain.Indexing
{
    public class FirstPassBlockCancelled
    {
        public string BlockchainId { get; set; }
        public string BlockId { get; set; }
        public long BlockNumber { get; set; }
    }
}