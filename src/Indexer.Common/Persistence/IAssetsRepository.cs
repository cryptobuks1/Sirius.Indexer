using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain;

namespace Indexer.Common.Persistence
{
    public interface IAssetsRepository
    {
        Task<IReadOnlyCollection<Asset>> GetAllAsync();
    }
}
