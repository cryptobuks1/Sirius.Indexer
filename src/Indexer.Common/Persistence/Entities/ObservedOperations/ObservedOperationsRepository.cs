using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.ObservedOperations;
using Microsoft.EntityFrameworkCore;

namespace Indexer.Common.Persistence.Entities.ObservedOperations
{
    public class ObservedOperationsRepository : IObservedOperationsRepository
    {
        private readonly IBlockchainDbConnectionFactory _connectionFactory;

        public ObservedOperationsRepository(IBlockchainDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task AddOrIgnore(ObservedOperation observedOperation)
        {
            await using var connection = await _connectionFactory.Create(observedOperation.BlockchainId);

            var schema = DbSchema.GetName(observedOperation.BlockchainId);
            var query = $@"
                insert into {schema}.{TableNames.ObserverOperations}
                (id, transaction_id, added_at)
                values (@id, @transactionId, @addedAt)";

            try
            {
                await connection.ExecuteAsync(query, new
                    {
                        id = observedOperation.Id,
                        transaction_id = observedOperation.TransactionId,
                        added_at = observedOperation.AddedAt
                    });
            }
            catch (DbUpdateException e) when (e.IsPrimaryKeyViolationException())
            {
            }
        }

        public async Task<IReadOnlyCollection<ObservedOperation>> GetInvolvedInBlock(string blockchainId, string blockId)
        {
            await using var connection = await _connectionFactory.Create(blockchainId);

            var schema = DbSchema.GetName(blockchainId);
            var query = $@"
                select o.* from {schema}.{TableNames.ObserverOperations} o
                join {schema}.{TableNames.TransactionHeaders} t on t.id = o.transaction_id
                where t.block_id = @blockId";

            var entities = await connection.QueryAsync<ObservedOperationEntity>(query, new {blockId});

            return entities
                .Select(x => ObservedOperation.Restore(
                    x.id,
                    blockchainId,
                    x.transaction_id,
                    x.added_at))
                .ToArray();
        }
    }
}
