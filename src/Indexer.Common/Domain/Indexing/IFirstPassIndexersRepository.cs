using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IFirstPassIndexersRepository
    {
        Task<FirstPassIndexer> Get(FirstPassIndexerId id);
        Task<FirstPassIndexer> GetOrDefault(FirstPassIndexerId id);
        Task Add(FirstPassIndexer indexer);
        Task<FirstPassIndexer> Update(FirstPassIndexer indexer);
        Task<IEnumerable<FirstPassIndexer>> GetByBlockchain(string blockchainId);
        Task Remove(string blockchainId);
    }
}
