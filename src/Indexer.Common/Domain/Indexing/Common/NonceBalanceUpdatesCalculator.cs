using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Domain.Indexing.Common
{
    public class NonceBalanceUpdatesCalculator
    {
        private readonly AssetsManager _assetsManager;

        public NonceBalanceUpdatesCalculator(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public async Task<IReadOnlyCollection<BalanceUpdate>> Calculate(NonceBlock block)
        {
            var operations = block.Transfers.SelectMany(tx => tx.Operations).ToArray();

            var sources = operations.SelectMany(x => x.Sources).ToArray();
            var destinations = operations.SelectMany(x => x.Destinations).ToArray();
            var feeSources = block.Transfers.SelectMany(x => x.Fees).ToArray();
            var blockBlockchainAssets = sources
                .Select(x => x.Unit.Asset)
                .Union(destinations.Select(x => x.Unit.Asset))
                .Union(feeSources.Select(x => x.BlockchainUnit.Asset))
                .ToArray();
            var blockAssets = await _assetsManager.EnsureAdded(block.Header.BlockchainId, blockBlockchainAssets);
            
            var balanceUpdates = new Dictionary<(string Address, long AssetId), decimal>();

            foreach (var transferSource in sources)
            {
                var key = (transferSource.Sender.Address, blockAssets[transferSource.Unit.Asset.Id].Id);

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
                var key = (transferDestination.Recipient.Address, blockAssets[transferDestination.Unit.Asset.Id].Id);

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
                var key = (feeSource.FeePayerAddress, blockAssets[feeSource.BlockchainUnit.Asset.Id].Id);

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
                    block.Header.Number,
                    block.Header.Id,
                    block.Header.MinedAt,
                    x.Value))
                .ToArray();
        }
    }
}
