using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Common
{
    public class NonceBlockAssetsProvider
    {
        private readonly AssetsManager _assetsManager;

        public NonceBlockAssetsProvider(AssetsManager assetsManager)
        {
            _assetsManager = assetsManager;
        }

        public async Task<IReadOnlyDictionary<BlockchainAssetId, Asset>> Get(
            string blockchainId,
            IReadOnlyCollection<TransferSource> sources, 
            IReadOnlyCollection<TransferDestination> destinations,
            IReadOnlyCollection<FeeSource> feeSources)
        {
            var blockBlockchainAssets = sources
                .Select(x => x.Unit.Asset)
                .Union(destinations.Select(x => x.Unit.Asset))
                .Union(feeSources.Select(x => x.BlockchainUnit.Asset))
                .ToArray();

            return await _assetsManager.EnsureAdded(blockchainId, blockBlockchainAssets);
        }
    }
}
