using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;

namespace IndexerTests.Mocks
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

        public Task<FirstPassIndexer> Update(FirstPassIndexer indexer)
        {
            lock (_store)
            {
                _store[indexer.Id] = indexer;
            }

            return Task.FromResult(indexer);
        }

        public Task<IEnumerable<FirstPassIndexer>> GetByBlockchain(string blockchainId)
        {
            lock (_store)
            {
                var indexers = _store.Where(x => x.Key.BlockchainId == blockchainId).Select(x => x.Value).ToArray();

                return Task.FromResult<IEnumerable<FirstPassIndexer>>(indexers);
            }
        }

        public Task Remove(string blockchainId)
        {
            lock (_store)
            {
                var keys = _store.Where(x => x.Key.BlockchainId == blockchainId).Select(x => x.Key).ToArray();

                foreach (var key in keys)
                {
                    _store.Remove(key);
                }
            }

            return Task.CompletedTask;
        }
    }
}
