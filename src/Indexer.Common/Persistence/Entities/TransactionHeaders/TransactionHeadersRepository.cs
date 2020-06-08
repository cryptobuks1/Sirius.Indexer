using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.Persistence.EntityFramework;
using Indexer.Common.Telemetry;
using Npgsql;
using NpgsqlTypes;
using PostgreSQLCopyHelper;

namespace Indexer.Common.Persistence.Entities.TransactionHeaders
{
    internal class TransactionHeadersRepository : ITransactionHeadersRepository
    {
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;
        private readonly IAppInsight _appInsight;

        public TransactionHeadersRepository(Func<Task<NpgsqlConnection>> connectionFactory,
            IAppInsight appInsight)
        {
            _connectionFactory = connectionFactory;
            _appInsight = appInsight;
        }

        public async Task InsertOrIgnore(IReadOnlyCollection<TransactionHeader> transactionHeaders)
        {
            if (!transactionHeaders.Any())
            {
                return;
            }
            
            await using var connection = await _connectionFactory.Invoke();
            
            var telemetry = _appInsight.StartSqlCopyCommand<TransactionHeader>();
            var schema = BlockchainSchema.Get(transactionHeaders.First().BlockchainId);
            var copyHelper = new PostgreSQLCopyHelper<TransactionHeader>(schema, TableNames.TransactionHeaders)
                .UsePostgresQuoting()
                .MapVarchar(nameof(TransactionHeaderEntity.block_id), p => p.BlockId)
                .MapVarchar(nameof(TransactionHeaderEntity.id), p => p.Id)
                .MapInteger(nameof(TransactionHeaderEntity.number), p => p.Number)
                .MapVarchar(nameof(TransactionHeaderEntity.error_message), p => p.Error?.Message)
                .MapNullable(nameof(TransactionHeaderEntity.error_code), p => p.Error?.Code, NpgsqlDbType.Integer);

            try
            {
                try
                {
                    await copyHelper.SaveAllAsync(connection, transactionHeaders);
                }
                catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
                {
                    var notExisted = await ExcludeExistingInDb(connection, transactionHeaders);

                    if (notExisted.Any())
                    {
                        await copyHelper.SaveAllAsync(connection, notExisted);
                    }
                }

                telemetry.Complete();
            }
            catch (Exception ex)
            {
                telemetry.Fail(ex);

                throw;
            }
        }

        public async Task RemoveByBlock(string blockchainId, string blockId)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = BlockchainSchema.Get(blockchainId);
            var query = $"delete from {schema}.{TableNames.TransactionHeaders} where bBlock_id = @blockId";

            await connection.ExecuteAsync(query, new {blockId});
        }

        private static async Task<IReadOnlyCollection<TransactionHeader>> ExcludeExistingInDb(
            NpgsqlConnection connection,
            IReadOnlyCollection<TransactionHeader> transactionHeaders)
        {
            if (!transactionHeaders.Any())
            {
                return Array.Empty<TransactionHeader>();
            }

            var schema = BlockchainSchema.Get(transactionHeaders.First().BlockchainId);
            var ids = transactionHeaders.Select(x => x.Id);
            var inList = string.Join("', '", ids);
            
            // limit is specified to avoid scanning indexes of the partitions once all headers are found
            var query = $"select id from {schema}.{TableNames.TransactionHeaders} where id in ('{inList}') limit @limit";
            var existingEntities = await connection.QueryAsync<TransactionHeaderEntity>(query, new {transactionHeaders.Count});

            var existingIds = existingEntities
                .Select(x => x.id)
                .ToHashSet();
            
            return transactionHeaders.Where(x => !existingIds.Contains(x.Id)).ToArray();
        }
    }
}
