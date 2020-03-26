namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class AssetAdded
    {
        public string AssetId { get; set; }
        public string BlockchainId { get; set; }
        public string Symbol { get; set; }
        /// <summary>
        /// Optional
        /// </summary>
        public string Address { get; set; }
        public int Accuracy { get; set; }
    }
}
