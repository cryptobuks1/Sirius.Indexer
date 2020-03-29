namespace Indexer.Common.ServiceFunctions
{
    public class PublishAsset
    {
        public long AssetId { get; set; }
        public string BlockchainId { get; set; }
        public string Symbol { get; set; }
        /// <summary>
        /// Optional
        /// </summary>
        public string Address { get; set; }
        public int Accuracy { get; set; }
    }
}
