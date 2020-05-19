using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface ISecondPassHistoryIndexersRepository
    {
        Task<SecondPassHistoryIndexer> Get(string blockchainId);
        Task<SecondPassHistoryIndexer> GetOrDefault(string blockchainId);
        Task Add(SecondPassHistoryIndexer indexer);
        Task Update(SecondPassHistoryIndexer indexer);
    }
}
