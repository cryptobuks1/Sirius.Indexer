using System.Collections.Generic;
using System.Linq;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Domain.Transactions.Transfers.Coins;

namespace Indexer.Common.Domain.Indexing.Common
{
    public static class CoinsBalanceUpdatesCalculator
    {
        public static IReadOnlyCollection<BalanceUpdate> Calculate(BlockHeader blockHeader,
            IEnumerable<UnspentCoin> blockOutputCoins,
            IEnumerable<SpentCoin> spentByBlockCoins)
        {
            var income = blockOutputCoins
                .Where(x => x.Address != null)
                .Select(x => new
                {
                    Address = x.Address,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (x.Address, x.AssetId))
                .Select(g => new
                {
                    Address = g.Key.Address,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var outcome = spentByBlockCoins
                .Where(x => x.Address != null)
                .Select(x => new
                {
                    Address = x.Address,
                    AssetId = x.Unit.AssetId,
                    Amount = x.Unit.Amount
                })
                .GroupBy(x => (x.Address, x.AssetId))
                .Select(g => new
                {
                    Address = g.Key.Address,
                    AssetId = g.Key.AssetId,
                    Amount = g.Sum(x => x.Amount)
                });

            var balanceUpdates = new Dictionary<(string Address, long AssetId), decimal>();

            foreach (var item in income)
            {
                balanceUpdates[(item.Address, item.AssetId)] = item.Amount;
            }

            foreach (var item in outcome)
            {
                var key = (item.Address, item.AssetId);

                if (balanceUpdates.TryGetValue(key, out var currentBalanceUpdate))
                {
                    balanceUpdates[key] = currentBalanceUpdate - item.Amount;
                }
                else
                {
                    balanceUpdates.Add(key, -item.Amount);
                }
            }

            foreach (var (balanceUpdateKey, balanceUpdate) in balanceUpdates.ToArray())
            {
                if (balanceUpdate == 0)
                {
                    balanceUpdates.Remove(balanceUpdateKey);
                }
            }

            return balanceUpdates
                .Select(x => BalanceUpdate.Create(
                    x.Key.Address,
                    x.Key.AssetId,
                    blockHeader.Number,
                    blockHeader.Id,
                    blockHeader.MinedAt,
                    x.Value))
                .ToArray();
        }
    }
}
