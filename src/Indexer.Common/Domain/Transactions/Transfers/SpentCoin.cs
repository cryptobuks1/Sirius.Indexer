﻿using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers
{
    public sealed class SpentCoin
    {
        public SpentCoin(CoinId id,
            Unit unit,
            string address,
            string tag,
            DestinationTagType? tagType,
            string spentByTransactionId,
            int spentByInputCoinNumber)
        {
            Id = id;
            Unit = unit;
            Address = address;
            Tag = tag;
            TagType = tagType;
            SpentByTransactionId = spentByTransactionId;
            SpentByInputCoinNumber = spentByInputCoinNumber;
        }

        public CoinId Id { get; }
        public Unit Unit { get; }
        public string Address { get; }
        public string Tag { get; }
        public DestinationTagType? TagType { get; }
        public string SpentByTransactionId { get; }
        public int SpentByInputCoinNumber { get; }
    }
}