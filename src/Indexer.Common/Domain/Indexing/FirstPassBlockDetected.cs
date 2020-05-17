namespace Indexer.Common.Domain.Indexing
{
    public class FirstPassBlockDetected
    {
        public string BlockchainId { get; set; }
        public string BlockId { get; set; }
        public long BlockNumber { get; set; }
        public string PreviousBlockId { get; set; }
    }
}