namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class BlockDetected
    {
        public string BlockchainId { get; set; }
        public string BlockId { get; set; }
        public long BlockNumber { get; set; }
        public string PreviousBlockId { get; set; }
        public long ChainSequence { get; set; }
    }
}
