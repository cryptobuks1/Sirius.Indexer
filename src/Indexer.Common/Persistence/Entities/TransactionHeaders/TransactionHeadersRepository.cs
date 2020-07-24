using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions;
using Npgsql;
using NpgsqlTypes;
using PostgreSQLCopyHelper;
using Swisschain.Extensions.Postgres;

namespace Indexer.Common.Persistence.Entities.TransactionHeaders
{
    internal class TransactionHeadersRepository : ITransactionHeadersRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;

        public TransactionHeadersRepository(NpgsqlConnection connection, string schema)
        {
            _connection = connection;
            _schema = schema;
        }

        public async Task InsertOrIgnore(IReadOnlyCollection<TransactionHeader> transactionHeaders)
        {
            if (!transactionHeaders.Any())
            {
                return;
            }

            var copyHelper = new PostgreSQLCopyHelper<TransactionHeader>(_schema, TableNames.TransactionHeaders)
                .UsePostgresQuoting()
                .MapVarchar(nameof(TransactionHeaderEntity.block_id), p => p.BlockId)
                .MapVarchar(nameof(TransactionHeaderEntity.id), p => p.Id)
                .MapInteger(nameof(TransactionHeaderEntity.number), p => p.Number)
                .MapVarchar(nameof(TransactionHeaderEntity.error_message), p => p.Error?.Message)
                .MapNullable(nameof(TransactionHeaderEntity.error_code), p => (int?)p.Error?.Code, NpgsqlDbType.Integer);

            try
            {
                await copyHelper.SaveAllAsync(_connection, transactionHeaders);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(transactionHeaders);

                if (notExisted.Any())
                {
                    await copyHelper.SaveAllAsync(_connection, notExisted);
                }
            }
        }

        public async Task RemoveByBlock(string blockId)
        {
            var query = $"delete from {_schema}.{TableNames.TransactionHeaders} where block_id = @blockId";

            await _connection.ExecuteAsync(query, new {blockId});
        }

        private async Task<IReadOnlyCollection<TransactionHeader>> ExcludeExistingInDb(IReadOnlyCollection<TransactionHeader> transactionHeaders)
        {
            if (!transactionHeaders.Any())
            {
                return Array.Empty<TransactionHeader>();
            }

            var existingEntities = await _connection.QueryInList<TransactionHeaderEntity, TransactionHeader>(
                _schema,
                TableNames.TransactionHeaders,
                transactionHeaders,
                columnsToSelect: "id",
                listColumns: "id",
                x => $"'{x.Id}'",
                knownSourceLength: transactionHeaders.Count);
            
            var existingIds = existingEntities
                .Select(x => x.id)
                .ToHashSet();
            
            return transactionHeaders.Where(x => !existingIds.Contains(x.Id)).ToArray();
        }
    }
}
