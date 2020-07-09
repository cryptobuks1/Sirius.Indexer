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
            var minted = blockOutputCoins
                .Select(x => new
                {
                    TransactionId = x.Id.TransactionId,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (x.TransactionId, x.AssetId))
                .Select(g => new
                {
                    TransactionId = g.Key.TransactionId,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var burned = spentByBlockCoins
                .Select(x => new
                {
                    TransactionId = x.SpentByCoinId.TransactionId,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (x.TransactionId, x.AssetId))
                .Select(g => new
                {
                    TransactionId = g.Key.TransactionId,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var fees = new Dictionary<(string TransactionId, long AssetId), decimal>();

            foreach (var item in burned)
            {
                fees[(item.TransactionId, item.AssetId)] = item.Amount;
            }

            foreach (var item in minted)
            {
                var key = (item.TransactionId, item.AssetId);

                if (fees.TryGetValue(key, out var currentFee))
                {
                    fees[key] = currentFee - item.Amount;
                }
                else
                {
                    fees.Add(key, -item.Amount);
                }
            }

            foreach (var (feeKey, fee) in fees.ToArray())
            {
                if (fee <= 0)
                {
                    fees.Remove(feeKey);
                }
            }

            return fees
                .Select(x => new Fee(
                    x.Key.TransactionId,
                    blockHeader.Id,
                    new Unit(x.Key.AssetId, x.Value)))
                .ToArray();
        }
    }
}
