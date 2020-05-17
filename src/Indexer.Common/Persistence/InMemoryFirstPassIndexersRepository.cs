using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;

namespace Indexer.Common.Persistence
{
    internal class InMemoryFirstPassIndexersRepository : IFirstPassIndexersRepository
    {
        private readonly Dictionary<FirstPassIndexerId, FirstPassIndexer> _store = new Dictionary<FirstPassIndexerId, FirstPassIndexer>();

        public Task<FirstPassIndexer> Get(FirstPassIndexerId id)
        {
            lock (_store)
            {
                return Task.FromResult(_store[id]);
            }
        }

        public Task<FirstPassIndexer> GetOrDefault(FirstPassIndexerId id)
        {
            lock (_store)
            {
                _store.TryGetValue(id, out var indexer);

                return Task.FromResult(indexer);
            }
        }

        public Task Add(FirstPassIndexer indexer)
        {
            lock (_store)
            {
                _store.Add(indexer.Id, indexer);
            }

            return Task.CompletedTask;
        }

        public Task Update(FirstPassIndexer indexer)
        {
            lock (_store)
            {
                _store[indexer.Id] = indexer;
            }

            return Task.CompletedTask;
        }
    }
}
