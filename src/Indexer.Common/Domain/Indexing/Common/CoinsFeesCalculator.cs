using System.Collections.Generic;
using System.Linq;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers.Coins;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Common
{
    public static class CoinsFeesCalculator
    {
        public static IReadOnlyCollection<Fee> Calculate(BlockHeader blockHeader,
            IEnumerable<UnspentCoin> blockOutputCoins,
            IEnumerable<SpentCoin> spentByBlockCoins)
        {
            var fees = new Dictionary<(string TransactionId, long AssetId), decimal>();

            foreach (var item in spentByBlockCoins)
            {
                var key = (item.SpentByCoinId.TransactionId, item.Unit.AssetId);

                if (fees.TryGetValue(key, out var currentFee))
                {
                    fees[key] = currentFee + item.Unit.Amount;
                }
                else
                {
                    fees.Add(key, item.Unit.Amount);
                }
            }

            foreach (var item in blockOutputCoins)
            {
                var key = (item.Id.TransactionId, item.Unit.AssetId);

                if (fees.TryGetValue(key, out var currentFee))
                {
                    fees[key] = currentFee - item.Unit.Amount;
                }
                else
                {
                    fees.Add(key, -item.Unit.Amount);
                }
            }

            return fees
                .Where(x => x.Value > 0)
                .Select(x => new Fee(
                    x.Key.TransactionId,
                    blockHeader.Id,
                    new Unit(x.Key.AssetId, x.Value)))
                .ToArray();
        }
    }
}
