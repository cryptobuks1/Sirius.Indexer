using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.Assets
{
    public interface IAssetsRepository
    {
        Task<IReadOnlyCollection<Asset>> GetAllAsync(string blockchainId);
        Task<IReadOnlyCollection<Asset>> GetExisting(string blockchainId,
            IReadOnlyCollection<BlockchainAssetId> blockchainAssetIds);
        Task Add(string blockchainId, IReadOnlyCollection<BlockchainAsset> blockchainAssets);
    }
}
