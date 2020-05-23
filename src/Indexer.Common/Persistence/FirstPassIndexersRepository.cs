using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

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
            var result = await GetOrDefault(id);

            if (result == null)
            {
                throw new InvalidOperationException($"First pass indexer not found {id}");
            }

            return result;
        }

        public async Task<FirstPassIndexer> GetOrDefault(FirstPassIndexerId id)
        {
            await using var context = _contextFactory.Invoke();

            var entity = await context.FirstPassHistoryIndexers.FindAsync(id.ToString());

            return entity != null ? MapFromEntity(entity) : null;
        }

        public async Task Add(FirstPassIndexer indexer)
        {
            await using var context = _contextFactory.Invoke();

            var entity = MapToEntity(indexer);

            await context.FirstPassHistoryIndexers.AddAsync(entity);

            await context.SaveChangesAsync();
        }
        
        public async Task<FirstPassIndexer> Update(FirstPassIndexer indexer)
        {
            await using var context = _contextFactory.Invoke();

            var entity = MapToEntity(indexer);

            context.FirstPassHistoryIndexers.Update(entity);

            await context.SaveChangesAsync();

            // Returns updated Version
            return MapFromEntity(entity);
        }

        public async Task<IEnumerable<FirstPassIndexer>> GetByBlockchain(string blockchainId)
        {
            await using var context = _contextFactory.Invoke();

            var entities = await context.FirstPassHistoryIndexers.Where(x => x.BlockchainId == blockchainId).ToArrayAsync();

            return entities.Select(MapFromEntity);
        }

        private static FirstPassIndexer MapFromEntity(FirstPassIndexerEntity entity)
        {
            return FirstPassIndexer.Restore(
                new FirstPassIndexerId(entity.BlockchainId, entity.StartBlock),
                entity.StopBlock,
                entity.NextBlock,
                entity.Version);
        }
        
        private static FirstPassIndexerEntity MapToEntity(FirstPassIndexer indexer)
        {
            return new FirstPassIndexerEntity
            {
                Id = indexer.Id.ToString(),
                BlockchainId = indexer.BlockchainId,
                StartBlock = indexer.StartBlock,
                StopBlock = indexer.StopBlock,
                NextBlock = indexer.NextBlock,
                Version = indexer.Version
            };
        }
    }
}
