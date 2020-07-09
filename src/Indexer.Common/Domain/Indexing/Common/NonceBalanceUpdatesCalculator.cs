using System.Collections.Generic;
using System.Linq;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Common
{
    public static class NonceBalanceUpdatesCalculator
    {
        public static IReadOnlyCollection<BalanceUpdate> Calculate(
            BlockHeader blockHeader,
            IReadOnlyCollection<TransferSource> sources, 
            IReadOnlyCollection<TransferDestination> destinations,
            IReadOnlyCollection<FeeSource> feeSources,
            IReadOnlyDictionary<BlockchainAssetId, Asset> assets)
        {
            var balanceUpdates = new Dictionary<(string Address, long AssetId), decimal>();

            foreach (var transferSource in sources)
            {
                var key = (transferSource.Sender.Address, assets[transferSource.Unit.Asset.Id].Id);

                if (balanceUpdates.TryGetValue(key, out var currentBalanceUpdate))
                {
                    balanceUpdates[key] = currentBalanceUpdate - transferSource.Unit.Amount;
                }
                else
                {
                    balanceUpdates.Add(key, -transferSource.Unit.Amount);
                }
            }

            foreach (var transferDestination in destinations)
            {
                var key = (transferDestination.Recipient.Address, assets[transferDestination.Unit.Asset.Id].Id);

                if (balanceUpdates.TryGetValue(key, out var currentBalanceUpdate))
                {
                    balanceUpdates[key] = currentBalanceUpdate + transferDestination.Unit.Amount;
                }
                else
                {
                    balanceUpdates.Add(key, transferDestination.Unit.Amount);
                }
            }

            foreach (var feeSource in feeSources)
            {
                var key = (feeSource.FeePayerAddress, assets[feeSource.BlockchainUnit.Asset.Id].Id);

                if (balanceUpdates.TryGetValue(key, out var currentBalanceUpdate))
                {
                    balanceUpdates[key] = currentBalanceUpdate - feeSource.BlockchainUnit.Amount;
                }
                else
                {
                    balanceUpdates.Add(key, -feeSource.BlockchainUnit.Amount);
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
