using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Persistence.DbContexts;

namespace Indexer.Common.Persistence
{
    internal class FirstPassIndexersRepository : IFirstPassIndexersRepository
    {
        private readonly Func<DatabaseContext> _contextFactory;

        public FirstPassIndexersRepository(Func<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        
        public async Task<FirstPassIndexer> Get(FirstPassIndexerId id)
        {
            
        }

        public async Task<FirstPassIndexer> GetOrDefault(FirstPassIndexerId id)
        {
            await using var context = _contextFactory.Invoke();

            context.
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

        public Task<IReadOnlyCollection<FirstPassIndexer>> GetByBlockchain(string blockchainId)
        {
            lock (_store)
            {
                var indexers = _store.Where(x => x.Key.BlockchainId == blockchainId).Select(x => x.Value).ToArray();

                return Task.FromResult<IReadOnlyCollection<FirstPassIndexer>>(indexers);
            }
        }
    }
}
