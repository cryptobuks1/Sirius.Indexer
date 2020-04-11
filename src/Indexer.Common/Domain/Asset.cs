namespace Indexer.Common.Domain
{
    public class Asset
    {
        public Asset(long assetId, string blockchainId, string symbol, string address, int accuracy)
        {
            AssetId = assetId;
            BlockchainId = blockchainId;
            Symbol = symbol;
            Address = address;
            Accuracy = accuracy;
        }

        public long AssetId { get; }
        public string BlockchainId { get; }
        public string Symbol { get; }
        public string Address { get; }
        public int Accuracy { get; }
    }
}
