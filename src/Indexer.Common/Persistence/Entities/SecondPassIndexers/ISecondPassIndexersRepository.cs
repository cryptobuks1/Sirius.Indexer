using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.SecondPass;

namespace Indexer.Common.Persistence.Entities.SecondPassIndexers
{
    public interface ISecondPassIndexersRepository
    {
        Task<SecondPassIndexer> Get(string blockchainId);
        Task<SecondPassIndexer> GetOrDefault(string blockchainId);
        Task Add(SecondPassIndexer indexer);
        Task<SecondPassIndexer> Update(SecondPassIndexer indexer);
        Task Remove(string blockchainId);
    }
}
