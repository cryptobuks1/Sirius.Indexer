using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain
{
    public class Asset
    {
        public Asset(AssetId assetId, BlockId blockchainId, Symbol symbol, Address address, int accuracy)
        {
            AssetId = assetId;
            BlockchainId = blockchainId;
            Symbol = symbol;
            Address = address;
            Accuracy = accuracy;
        }

        public AssetId AssetId { get; }
        public BlockId BlockchainId { get; }
        public Symbol Symbol { get; }
        public Address Address { get; }
        public int Accuracy { get; }
    }
}
