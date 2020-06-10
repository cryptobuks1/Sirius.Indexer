using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Assets
{
    public class Asset
    {
        private Asset(long id, string blockchainId, string symbol, string address, int accuracy)
        {
            Id = id;
            BlockchainId = blockchainId;
            Symbol = symbol;
            Address = address;
            Accuracy = accuracy;
        }

        public long Id { get; }
        public string BlockchainId { get; }
        public string Symbol { get; }
        public string Address { get; }
        public int Accuracy { get; }

        public static Asset Restore(long id,
            string blockchainId,
            string symbol,
            string address,
            int accuracy)
        {
            return new Asset(id,
                blockchainId,
                symbol,
                address,
                accuracy);
        }

        public BlockchainAssetId GetBlockchainAssetId()
        {
            return new BlockchainAssetId(Symbol, Address);
        }
    }
}
