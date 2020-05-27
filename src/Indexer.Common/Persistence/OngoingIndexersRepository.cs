using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.Persistence.Entities;

namespace Indexer.Common.Persistence
{
    internal sealed class OngoingIndexersRepository : IOngoingIndexersRepository
    {
        private readonly Func<DatabaseContext> _contextFactory;

        public OngoingIndexersRepository(Func<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<OngoingIndexer> Get(string blockchainId)
        {
            var result = await GetOrDefault(blockchainId);

            if (result == null)
            {
                throw new InvalidOperationException($"Ongoing indexer not found: {blockchainId}");
            }

            return result;
        }

        public async Task<OngoingIndexer> GetOrDefault(string blockchainId)
        {
            await using var context = _contextFactory.Invoke();

            var entity = await context.OngoingIndexers.FindAsync(blockchainId);

            return entity != null ? MapFromEntity(entity) : null;
        }

        public async Task Add(OngoingIndexer indexer)
        {
            await using var context = _contextFactory.Invoke();

            var entity = MapToEntity(indexer);

            await context.OngoingIndexers.AddAsync(entity);

            await context.SaveChangesAsync();
        }

        public async Task<OngoingIndexer> Update(OngoingIndexer indexer)
        {
            await using var context = _contextFactory.Invoke();

            var entity = MapToEntity(indexer);

            context.OngoingIndexers.Update(entity);

            await context.SaveChangesAsync();

            // Updates Version
            return MapFromEntity(entity);
        }

        private static OngoingIndexer MapFromEntity(OngoingIndexerEntity entity)
        {
            return OngoingIndexer.Restore(
                entity.BlockchainId,
                entity.StartBlock,
                entity.NextBlock,
                entity.Sequence,
                entity.StartedAt.UtcDateTime,
                entity.UpdatedAt.UtcDateTime,
                entity.Version);
        }

        private static OngoingIndexerEntity MapToEntity(OngoingIndexer indexer)
        {
            return new OngoingIndexerEntity
            {
                BlockchainId = indexer.BlockchainId,
                StartBlock = indexer.StartBlock,
                NextBlock = indexer.NextBlock,
                Sequence = indexer.Sequence,
                StartedAt = indexer.StartedAt,
                UpdatedAt = indexer.UpdatedAt,
                Version = indexer.Version
            };
        }
    }
}
