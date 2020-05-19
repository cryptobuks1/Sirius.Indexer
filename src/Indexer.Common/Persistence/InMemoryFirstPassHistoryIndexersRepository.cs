using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;

namespace Indexer.Common.Persistence
{
    internal class InMemoryFirstPassHistoryIndexersRepository : IFirstPassHistoryIndexersRepository
    {
        private readonly Dictionary<FirstPassHistoryIndexerId, FirstPassHistoryIndexer> _store = new Dictionary<FirstPassHistoryIndexerId, FirstPassHistoryIndexer>();

        public Task<FirstPassHistoryIndexer> Get(FirstPassHistoryIndexerId id)
        {
            lock (_store)
            {
                return Task.FromResult(_store[id]);
            }
        }

        public Task<FirstPassHistoryIndexer> GetOrDefault(FirstPassHistoryIndexerId id)
        {
            lock (_store)
            {
                _store.TryGetValue(id, out var indexer);

                return Task.FromResult(indexer);
            }
        }

        public Task Add(FirstPassHistoryIndexer indexer)
        {
            lock (_store)
            {
                _store.Add(indexer.Id, indexer);
            }

            return Task.CompletedTask;
        }

        public Task Update(FirstPassHistoryIndexer indexer)
        {
            lock (_store)
            {
                _store[indexer.Id] = indexer;
            }

            return Task.CompletedTask;
        }
    }
}
