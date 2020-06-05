using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.Ongoing;

namespace Indexer.Common.Persistence.Entities.OngoingIndexers
{
    public interface IOngoingIndexersRepository
    {
        Task<OngoingIndexer> Get(string blockchainId);
        Task<OngoingIndexer> Update(OngoingIndexer indexer);
        Task<OngoingIndexer> GetOrDefault(string blockchainId);
        Task Add(OngoingIndexer indexer);
        Task Remove(string blockchainId);
    }
}
