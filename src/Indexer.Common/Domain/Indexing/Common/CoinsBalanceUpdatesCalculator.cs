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
            var balanceUpdates = new Dictionary<(string Address, long AssetId), decimal>();

            foreach (var item in blockOutputCoins.Where(x => x.Address != null))
            {
                var key = (item.Address, item.Unit.AssetId);

                if (balanceUpdates.TryGetValue(key, out var currentBalanceUpdate))
                {
                    balanceUpdates[key] = currentBalanceUpdate + item.Unit.Amount;
                }
                else
                {
                    balanceUpdates.Add(key, item.Unit.Amount);
                }
            }

            foreach (var item in spentByBlockCoins.Where(x => x.Address != null))
            {
                var key = (item.Address, item.Unit.AssetId);

                if (balanceUpdates.TryGetValue(key, out var currentBalanceUpdate))
                {
                    balanceUpdates[key] = currentBalanceUpdate - item.Unit.Amount;
                }
                else
                {
                    balanceUpdates.Add(key, -item.Unit.Amount);
                }
            }

            return balanceUpdates
                .Where(x => x.Value != 0)
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
