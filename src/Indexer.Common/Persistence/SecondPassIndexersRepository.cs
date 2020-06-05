using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Persistence.Entities;
using Indexer.Common.Persistence.EntityFramework;
using Z.EntityFramework.Plus;

namespace Indexer.Common.Persistence
{
    internal sealed class SecondPassIndexersRepository : ISecondPassIndexersRepository
    {
        private readonly Func<DatabaseContext> _contextFactory;

        public SecondPassIndexersRepository(Func<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<SecondPassIndexer> Get(string blockchainId)
        {
            var result = await GetOrDefault(blockchainId);

            if (result == null)
            {
                throw new InvalidOperationException($"Second-pass indexer not found: {blockchainId}");
            }

            return result;
        }

        public async Task<SecondPassIndexer> GetOrDefault(string blockchainId)
        {
            await using var context = _contextFactory.Invoke();

            var entity = await context.SecondPassIndexers.FindAsync(blockchainId);

            return entity != null ? MapFromEntity(entity) : null;
        }

        public async Task Add(SecondPassIndexer indexer)
        {
            await using var context = _contextFactory.Invoke();

            var entity = MapToEntity(indexer);

            await context.SecondPassIndexers.AddAsync(entity);

            await context.SaveChangesAsync();
        }

        public async Task<SecondPassIndexer> Update(SecondPassIndexer indexer)
        {
            await using var context = _contextFactory.Invoke();

            var entity = MapToEntity(indexer);

            context.SecondPassIndexers.Update(entity);

            await context.SaveChangesAsync();

            // Updates Version
            return MapFromEntity(entity);
        }

        public async Task Remove(string blockchainId)
        {
            await using var context = _contextFactory.Invoke();

            await context.SecondPassIndexers.Where(x => x.BlockchainId == blockchainId).DeleteAsync();
        }

        private static SecondPassIndexer MapFromEntity(SecondPassIndexerEntity entity)
        {
            return SecondPassIndexer.Restore(
                entity.BlockchainId,
                entity.NextBlock,
                entity.StopBlock,
                entity.StartedAt.UtcDateTime,
                entity.UpdatedAt.UtcDateTime,
                entity.Version);
        }

        private static SecondPassIndexerEntity MapToEntity(SecondPassIndexer indexer)
        {
            return new SecondPassIndexerEntity
            {
                BlockchainId = indexer.BlockchainId,
                NextBlock = indexer.NextBlock,
                StopBlock = indexer.StopBlock,
                StartedAt = indexer.StartedAt,
                UpdatedAt = indexer.UpdatedAt,
                Version = indexer.Version
            };
        }
    }
}
