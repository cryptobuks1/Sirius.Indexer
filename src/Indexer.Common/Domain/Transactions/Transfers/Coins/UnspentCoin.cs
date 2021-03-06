﻿using System;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Transactions.Transfers.Coins
{
    public sealed class UnspentCoin : IEquatable<UnspentCoin>
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

        public bool Equals(UnspentCoin other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Id, other.Id) && Equals(Unit, other.Unit) && Address == other.Address && Tag == other.Tag && TagType == other.TagType;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is UnspentCoin other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id,
                Unit,
                Address,
                Tag,
                TagType);
        }
    }
}
