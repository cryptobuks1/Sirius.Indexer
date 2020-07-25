using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Persistence.Entities.OngoingIndexers;

namespace IndexerTests.Sdk.Mocks.Persistence
{
    internal sealed class InMemoryOngoingIndexersRepository : IOngoingIndexersRepository
    {
        private readonly Dictionary<string, OngoingIndexer> _store = new Dictionary<string, OngoingIndexer>();

        public Task<OngoingIndexer> Get(string blockchainId)
        {
            lock (_store)
            {
                return Task.FromResult(_store[blockchainId]);
            }
        }

        public Task<OngoingIndexer> Update(OngoingIndexer indexer)
        {
            lock (_store)
            {
                _store[indexer.BlockchainId] = indexer;
            }

            return Task.FromResult(indexer);
        }

        public Task<OngoingIndexer> GetOrDefault(string blockchainId)
        {
            lock (_store)
            {
                _store.TryGetValue(blockchainId, out var indexer);

                return Task.FromResult(indexer);
            }
        }

        public Task Add(OngoingIndexer indexer)
        {
            lock (_store)
            {
                _store.Add(indexer.BlockchainId, indexer);
            }

            return Task.CompletedTask;
        }

        public Task Remove(string blockchainId)
        {
            lock (_store)
            {
                _store.Remove(blockchainId);
            }

            return Task.CompletedTask;
        }
    }
}
