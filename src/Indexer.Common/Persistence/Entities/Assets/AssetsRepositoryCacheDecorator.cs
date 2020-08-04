using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.Assets
{
    internal sealed class AssetsRepositoryCacheDecorator : IAssetsRepository
    {
        private readonly IAssetsRepository _impl;
        private readonly ConcurrentDictionary<(string BlockchainId, BlockchainAssetId BlockchainAssetId), Asset> _cache;

        public AssetsRepositoryCacheDecorator(IAssetsRepository impl)
        {
            _impl = impl;
            // TODO: RLE cache with the limited size
            _cache = new ConcurrentDictionary<(string BlockchainId, BlockchainAssetId BlockchainAssetId), Asset>();
        }

        public Task<IReadOnlyCollection<Asset>> GetAllAsync(string blockchainId)
        {
            return _impl.GetAllAsync(blockchainId);
        }

        public async Task<IReadOnlyCollection<Asset>> GetExisting(string blockchainId,
            IReadOnlyCollection<BlockchainAssetId> blockchainAssetIds)
        {
            var cachedAssets = blockchainAssetIds
                .Select(x =>
                {
                    _cache.TryGetValue((BlockchainId: blockchainId, BlockchainAssetId: x), out var asset);

                    return asset;
                })
                .Where(x => x != null)
                .ToArray();

            if (cachedAssets.Length == blockchainAssetIds.Count)
            {
                return cachedAssets;
            }

            var cachedBlockchainAssetIds = cachedAssets
                .Select(x => x.GetBlockchainAssetId())
                .ToHashSet();

            var missedBlockchainAssetIds = blockchainAssetIds
                .Where(x => !cachedBlockchainAssetIds.Contains(x))
                .ToArray();

            var readAssets = await _impl.GetExisting(blockchainId, missedBlockchainAssetIds);

            foreach (var asset in readAssets)
            {
                _cache.TryAdd((BlockchainId: blockchainId, BlockchainAssetId: asset.GetBlockchainAssetId()), asset);
            }

            return cachedAssets.Concat(readAssets).ToArray();
        }

        public Task Add(string blockchainId, IReadOnlyCollection<BlockchainAsset> blockchainAssets)
        {
            var cachedAssets = blockchainAssets
                .Select(x =>
                {
                    _cache.TryGetValue((BlockchainId: blockchainId, BlockchainAssetId: x.Id), out var asset);

                    return asset;
                })
                .Where(x => x != null)
                .ToArray();

            if (cachedAssets.Length == blockchainAssets.Count)
            {
                return Task.CompletedTask;
            }

            var cachedBlockchainAssetIds = cachedAssets
                .Select(x => x.GetBlockchainAssetId())
                .ToHashSet();

            var missedBlockchainAssets = blockchainAssets
                .Where(x => !cachedBlockchainAssetIds.Contains(x.Id))
                .ToArray();

            return _impl.Add(blockchainId, missedBlockchainAssets);
        }
    }
}
