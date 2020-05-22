using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;

namespace IndexerTests.Mocks
{
    internal sealed class InMemorySecondPassHistoryIndexersRepository : ISecondPassHistoryIndexersRepository
    {
        private readonly Dictionary<string, SecondPassHistoryIndexer> _store = new Dictionary<string, SecondPassHistoryIndexer>();

        public Task<SecondPassHistoryIndexer> Get(string blockchainId)
        {
            lock (_store)
            {
                return Task.FromResult(_store[blockchainId]);
            }
        }

        public Task<SecondPassHistoryIndexer> GetOrDefault(string blockchainId)
        {
            lock (_store)
            {
                _store.TryGetValue(blockchainId, out var indexer);

                return Task.FromResult(indexer);
            }
        }

        public Task Add(SecondPassHistoryIndexer indexer)
        {
            lock (_store)
            {
                _store.Add(indexer.BlockchainId, indexer);
            }

            return Task.CompletedTask;
        }

        public Task Update(SecondPassHistoryIndexer indexer)
        {
            lock (_store)
            {
                _store[indexer.BlockchainId] = indexer;
            }

            return Task.CompletedTask;
        }
    }
}
