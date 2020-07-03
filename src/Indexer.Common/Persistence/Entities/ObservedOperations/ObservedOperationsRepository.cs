using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.ObservedOperations;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Indexer.Common.Persistence.Entities.ObservedOperations
{
    public class ObservedOperationsRepository : IObservedOperationsRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;
        private readonly string _blockchainId;

        public ObservedOperationsRepository(NpgsqlConnection connection, string schema, string blockchainId)
        {
            _connection = connection;
            _schema = schema;
            _blockchainId = blockchainId;
        }

        public async Task AddOrIgnore(ObservedOperation observedOperation)
        {
            var query = $@"
                insert into {_schema}.{TableNames.ObserverOperations}
                (id, transaction_id, added_at)
                values (@id, @transactionId, @addedAt)";

            try
            {
                await _connection.ExecuteAsync(query, new
                    {
                        id = observedOperation.Id,
                        transactionId = observedOperation.TransactionId,
                        addedAt = observedOperation.AddedAt
                    });
            }
            catch (DbUpdateException e) when (e.IsPrimaryKeyViolationException())
            {
            }
        }

        public async Task<IReadOnlyCollection<ObservedOperation>> GetInvolvedInBlock(string blockId)
        {
            var query = $@"
                select o.* from {_schema}.{TableNames.ObserverOperations} o
                join {_schema}.{TableNames.TransactionHeaders} t on t.id = o.transaction_id
                where t.block_id = @blockId";

            var entities = await _connection.QueryAsync<ObservedOperationEntity>(query, new {blockId});

            return entities
                .Select(x => ObservedOperation.Restore(
                    x.id,
                    _blockchainId,
                    x.transaction_id,
                    x.added_at))
                .ToArray();
        }
    }
}
