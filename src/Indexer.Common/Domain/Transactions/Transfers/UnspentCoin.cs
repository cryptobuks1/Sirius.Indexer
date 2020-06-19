using System;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class UnspentCoin
    {
        public UnspentCoin(CoinId id,
            Unit unit,
            string address,
            string tag,
            DestinationTagType? tagType)
        {
            Id = id;
            Unit = unit;
            Address = address;
            Tag = tag;
            TagType = tagType;
        }

        public CoinId Id { get; }
        public Unit Unit { get; }
        public string Address { get; }
        public string Tag { get; }
        public DestinationTagType? TagType { get; }

        public SpentCoin Spend(InputCoin byInputCoin)
        {
            if (byInputCoin.Type != InputCoinType.Regular)
            {
                throw new InvalidOperationException($"Coin {Id.TransactionId}:{Id.Number} can't be spent by input coin {byInputCoin.Id.TransactionId}:{byInputCoin.Id.Number} because input coin type is {byInputCoin.Type}");
            }

            return new SpentCoin(
                Id,
                Unit,
                Address,
                Tag,
                TagType,
                byInputCoin.Id);
        }
    }
}
