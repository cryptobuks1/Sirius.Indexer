using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Persistence.Entities.Assets;
using MassTransit;
using Swisschain.Sirius.Indexer.MessagingContract;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Assets
{
    public sealed class AssetsManager
    {
        private readonly IAssetsRepository _assetsRepository;
        private readonly IPublishEndpoint _publisher;

        public AssetsManager(IAssetsRepository assetsRepository,
            IPublishEndpoint publisher)
        {
            _assetsRepository = assetsRepository;
            _publisher = publisher;
        }

        public async Task<IReadOnlyDictionary<BlockchainAssetId, Asset>> EnsureAdded(string blockchainId, 
            IReadOnlyCollection<BlockchainAsset> blockchainAssets)
        {
            var existingAssets = await _assetsRepository.GetExisting(blockchainId, blockchainAssets.Select(x => x.Id).ToArray());
            var existingBlockchainAssetIds = existingAssets
                .Select(x => x.GetBlockchainAssetId())
                .ToHashSet();

            if (existingBlockchainAssetIds.Count == blockchainAssets.Count)
            {
                return existingAssets.ToDictionary(x => x.GetBlockchainAssetId());
            }

            var notExistingBlockchainAssets = blockchainAssets.Where(x => !existingBlockchainAssetIds.Contains(x.Id)).ToArray();
            
            await _assetsRepository.Add(blockchainId, notExistingBlockchainAssets);
            var newAssets = await _assetsRepository.GetExisting(blockchainId, notExistingBlockchainAssets.Select(x => x.Id).ToArray());
            
            var publishTasks = newAssets.Select(asset => _publisher.Publish(new AssetAdded
            {
                AssetId = asset.Id,
                BlockchainId = asset.BlockchainId,
                Symbol = asset.Symbol,
                Address = asset.Address,
                Accuracy = asset.Accuracy
            }));

            await Task.WhenAll(publishTasks);

            return existingAssets
                .Concat(newAssets)
                .ToDictionary(x => x.GetBlockchainAssetId());
        }
    }
}
