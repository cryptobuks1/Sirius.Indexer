using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain;

namespace Indexer.Common.Persistence.Entities.Assets
{
    public interface IAssetsRepository
    {
        Task<IReadOnlyCollection<Asset>> GetAllAsync();
        Task<Asset> GetAsync(long assetId);
    }
}
