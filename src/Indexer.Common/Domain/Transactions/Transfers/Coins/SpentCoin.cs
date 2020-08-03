using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers.Coins
{
    public sealed class SpentCoin
    {
        public SpentCoin(CoinId id,
            Unit unit,
            string address,
            string tag,
            DestinationTagType? tagType,
            CoinId spentByCoinId)
        {
            Id = id;
            Unit = unit;
            Address = address;
            Tag = tag;
            TagType = tagType;
            SpentByCoinId = spentByCoinId;
        }

        public CoinId Id { get; }
        public Unit Unit { get; }
        public string Address { get; }
        public string Tag { get; }
        public DestinationTagType? TagType { get; }
        public CoinId SpentByCoinId { get; }

        public UnspentCoin Revert()
        {
            return new UnspentCoin(
                Id,
                Unit,
                Address,
                Tag,
                TagType);
        }
    }
}
