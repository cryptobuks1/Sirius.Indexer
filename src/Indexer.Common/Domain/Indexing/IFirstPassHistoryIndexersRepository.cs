using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IFirstPassHistoryIndexersRepository
    {
        Task<FirstPassHistoryIndexer> Get(FirstPassHistoryIndexerId id);
        Task<FirstPassHistoryIndexer> GetOrDefault(FirstPassHistoryIndexerId id);
        Task Add(FirstPassHistoryIndexer indexer);
        Task Update(FirstPassHistoryIndexer indexer);
    }
}
