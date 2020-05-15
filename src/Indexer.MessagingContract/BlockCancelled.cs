namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class BlockCancelled
    {
        public string BlockchainId { get; set; }
        public string BlockId { get; set; }
        public long BlockNumber { get; set; }
    }
}
