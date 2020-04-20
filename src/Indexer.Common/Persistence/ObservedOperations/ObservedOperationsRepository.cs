using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Indexer.Common.Domain.ObservedOperations;
using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Indexer.Common.Persistence.ObservedOperations
{
    public class ObservedOperationsRepository : IObservedOperationsRepository
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;

        public ObservedOperationsRepository(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task AddOrIgnore(ObservedOperation observedOperation)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);
            var entity = MapToEntity(observedOperation);

            try
            {
                context.ObservedOperations.Add(entity);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException e) when( e.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
            }
        }

        private static ObservedOperationEntity MapToEntity(ObservedOperation observedOperation)
        {
            return new ObservedOperationEntity()
            {
                IsCompleted = observedOperation.IsCompleted,
                OperationId = observedOperation.OperationId,
                BlockchainId = observedOperation.BlockchainId,
                TransactionId = observedOperation.TransactionId
            };
        }
    }
}
