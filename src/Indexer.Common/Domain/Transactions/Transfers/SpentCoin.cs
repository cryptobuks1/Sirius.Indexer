using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class SpentCoin
    {
        public SpentCoin(CoinId id,
            Unit unit,
            string address,
            string scriptPubKey,
            string tag,
            DestinationTagType? tagType,
            CoinId spentByCoinId)
        {
            Id = id;
            Unit = unit;
            Address = address;
            ScriptPubKey = scriptPubKey;
            Tag = tag;
            TagType = tagType;
            SpentByCoinId = spentByCoinId;
        }

        public CoinId Id { get; }
        public Unit Unit { get; }
        public string Address { get; }
        public string ScriptPubKey { get; }
        public string Tag { get; }
        public DestinationTagType? TagType { get; }
        public CoinId SpentByCoinId { get; }

        public UnspentCoin Revert()
        {
            return new UnspentCoin(
                Id,
                Unit,
                Address,
                ScriptPubKey,
                Tag,
                TagType);
        }
    }
}
