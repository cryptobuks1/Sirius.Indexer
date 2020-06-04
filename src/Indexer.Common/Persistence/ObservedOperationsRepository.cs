using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.ObservedOperations;
using Indexer.Common.Persistence.Entities;
using Indexer.Common.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Indexer.Common.Persistence.ObservedOperations
{
    public class ObservedOperationsRepository : IObservedOperationsRepository
    {
        private readonly Func<DatabaseContext> _contextFactory;

        public ObservedOperationsRepository(Func<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task AddOrIgnore(ObservedOperation observedOperation)
        {
            await using var context = _contextFactory.Invoke();
            var entity = MapToEntity(observedOperation);

            try
            {
                context.ObservedOperations.Add(entity);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
            }
        }

        public async Task<IReadOnlyCollection<ObservedOperation>> GetExecutingAsync(long? cursor, int limit)
        {
            await using var context = _contextFactory.Invoke();

            var query = context.ObservedOperations.Where(x =>
                !x.IsCompleted).Select(x => x);

            if (cursor != null)
            {
                query = query.Where(x => x.OperationId > cursor);
            }

            return (await query
                    .OrderBy(x => x.OperationId)
                    .Take(limit)
                    .ToArrayAsync())
                .Select(MapFromEntity)
                .ToArray();
        }

        public async Task UpdateBatchAsync(IReadOnlyCollection<ObservedOperation> updatedOperations)
        {
            await using var context = _contextFactory.Invoke();

            var entities = updatedOperations.Select(MapToEntity);

            foreach (var entity in entities)
            {
                context.Attach(entity).State = EntityState.Modified;
            }

            await context.SaveChangesAsync();
        }

        private static ObservedOperationEntity MapToEntity(ObservedOperation observedOperation)
        {
            return new ObservedOperationEntity()
            {
                IsCompleted = observedOperation.IsCompleted,
                OperationId = observedOperation.OperationId,
                BlockchainId = observedOperation.BlockchainId,
                TransactionId = observedOperation.TransactionId,
                Fees = observedOperation.Fees,
                AssetId = observedOperation.AssetId,
                BilV1OperationId = observedOperation.BilV1OperationId,
                DestinationAddress = observedOperation.DestinationAddress,
                OperationAmount = observedOperation.OperationAmount,
            };
        }

        private static ObservedOperation MapFromEntity(ObservedOperationEntity observedOperationEntity)
        {
            return ObservedOperation.Restore(
                observedOperationEntity.OperationId,
                observedOperationEntity.BlockchainId,
                observedOperationEntity.TransactionId,
                observedOperationEntity.IsCompleted,
                observedOperationEntity.AssetId,
                observedOperationEntity.BilV1OperationId,
                observedOperationEntity.Fees,
                observedOperationEntity.DestinationAddress,
                observedOperationEntity.OperationAmount);
        }
    }
}
