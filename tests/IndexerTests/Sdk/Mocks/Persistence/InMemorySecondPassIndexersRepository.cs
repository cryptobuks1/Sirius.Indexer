﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.SecondPass;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;

namespace IndexerTests.Sdk.Mocks.Persistence
{
    internal sealed class InMemorySecondPassIndexersRepository : ISecondPassIndexersRepository
    {
        private readonly Dictionary<string, SecondPassIndexer> _store = new Dictionary<string, SecondPassIndexer>();

        public Task<SecondPassIndexer> Get(string blockchainId)
        {
            lock (_store)
            {
                return Task.FromResult(_store[blockchainId]);
            }
        }

        public Task<SecondPassIndexer> GetOrDefault(string blockchainId)
        {
            lock (_store)
            {
                _store.TryGetValue(blockchainId, out var indexer);

                return Task.FromResult(indexer);
            }
        }

        public Task Add(SecondPassIndexer indexer)
        {
            lock (_store)
            {
                _store.Add(indexer.BlockchainId, indexer);
            }

            return Task.CompletedTask;
        }

        public Task<SecondPassIndexer> Update(SecondPassIndexer indexer)
        {
            lock (_store)
            {
                _store[indexer.BlockchainId] = indexer;
            }

            return Task.FromResult(indexer);
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
