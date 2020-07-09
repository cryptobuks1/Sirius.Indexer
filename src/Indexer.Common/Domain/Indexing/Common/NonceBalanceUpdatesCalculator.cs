using System;
using System.Collections.Generic;
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

        public Task<IReadOnlyCollection<BalanceUpdate>> Calculate(NonceBlock block)
        {
            throw new NotImplementedException();

            //var operations = block.Transfers.SelectMany(tx => tx.Operations).ToArray();

            //var sources = operations.SelectMany(x => x.Sources).ToArray();
            //var destinations = operations.SelectMany(x => x.Destinations).ToArray();
            //var blockBlockchainAssets = sources
            //    .Select(x => x.Unit.Asset)
            //    .Concat(destinations.Select(x => x.Unit.Asset))
            //    .ToArray();

            //var blockAssets = await _assetsManager.EnsureAdded(block.Header.BlockchainId, blockBlockchainAssets);

            //// Just do two foreach for dest and src? (In coins the same)

            //var income = destinations
            //    .Select(x => new
            //    {
            //        Address = x.Recipient.Address,
            //        AssetId = blockAssets[x.Unit.Asset.Id].Id,
            //        Amount = x.Unit.Amount
            //    })
            //    .GroupBy(x => new { x.Address, x.AssetId})
            //    .Select(g => new
            //    {
            //        Address = g.Key.Address,
            //        AssetId = g.Key.AssetId,
            //        Amount = g.Sum(x => x.Amount)
            //    });
        }
    }
}
