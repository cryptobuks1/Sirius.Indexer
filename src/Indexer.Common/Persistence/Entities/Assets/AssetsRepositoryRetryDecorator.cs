using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Durability;
using Polly.Retry;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.Assets
{
    internal sealed class AssetsRepositoryRetryDecorator : IAssetsRepository
    {
        private readonly IAssetsRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public AssetsRepositoryRetryDecorator(IAssetsRepository impl)
        {
            _impl = impl;
            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }
        
        public Task<IReadOnlyCollection<Asset>> GetAllAsync(string blockchainId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetAllAsync(blockchainId));
        }

        public Task<IReadOnlyCollection<Asset>> GetExisting(string blockchainId,
            IReadOnlyCollection<BlockchainAssetId> blockchainAssetIds)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetExisting(blockchainId, blockchainAssetIds));
        }

        public Task Add(string blockchainId, IReadOnlyCollection<BlockchainAsset> blockchainAssets)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Add(blockchainId, blockchainAssets));
        }
    }
}