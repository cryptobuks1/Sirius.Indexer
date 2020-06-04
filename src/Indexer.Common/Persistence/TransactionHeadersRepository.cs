using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Persistence.Entities;
using Indexer.Common.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using PostgreSQLCopyHelper;
using Z.EntityFramework.Plus;

namespace Indexer.Common.Persistence
{
    internal class TransactionHeadersRepository : ITransactionHeadersRepository
    {
        private static readonly PostgreSQLCopyHelper<TransactionHeaderEntity> CopyHelper;

        private readonly Func<DatabaseContext> _contextFactory;

        static TransactionHeadersRepository()
        {
            CopyHelper = new PostgreSQLCopyHelper<TransactionHeaderEntity>(DatabaseContext.SchemaName, TableNames.TransactionHeaders)
                .UsePostgresQuoting()
                .MapVarchar(nameof(TransactionHeaderEntity.GlobalId), p => p.GlobalId)
                .MapVarchar(nameof(TransactionHeaderEntity.BlockchainId), p => p.BlockchainId)
                .MapVarchar(nameof(TransactionHeaderEntity.BlockId), p => p.BlockId)
                .MapVarchar(nameof(TransactionHeaderEntity.Id), p => p.Id)
                .MapInteger(nameof(TransactionHeaderEntity.Number), p => p.Number)
                .MapVarchar(nameof(TransactionHeaderEntity.ErrorMessage), p => p.ErrorMessage)
                .MapNullable(nameof(TransactionHeaderEntity.ErrorCode), p => p.ErrorCode, NpgsqlDbType.Integer);
        }

        public TransactionHeadersRepository(Func<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task InsertOrIgnore(IEnumerable<TransactionHeader> transactionHeaders)
        {
            var entities = transactionHeaders
                .Select(MapToEntity)
                .ToArray();

            if (!entities.Any())
            {
                return;
            }
            
            await using var context = _contextFactory.Invoke();
            await using var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            
            var telemetry = context.AppInsight.StartSqlCopyCommand<TransactionHeaderEntity>();

            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                try
                {
                    await CopyHelper.SaveAllAsync(connection, entities);
                }
                catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
                {
                    var notExisted = await ExcludeExistingInDb(context, entities);

                    if (notExisted.Any())
                    {
                        await CopyHelper.SaveAllAsync(connection, notExisted);
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
            await using var context = _contextFactory.Invoke();

            await context.TransactionHeaders
                .Where(x => x.BlockchainId == blockchainId && x.BlockId == blockId)
                .DeleteAsync();
        }

        private static TransactionHeaderEntity MapToEntity(TransactionHeader transactionHeader)
        {
            return new TransactionHeaderEntity
            {
                GlobalId = transactionHeader.GlobalId,
                BlockchainId = transactionHeader.BlockchainId,
                BlockId = transactionHeader.BlockId,
                Id = transactionHeader.Id,
                Number = transactionHeader.Number,
                ErrorMessage = transactionHeader.Error?.Message,
                ErrorCode = transactionHeader.Error?.Code
            };
        }

        private static async Task<IReadOnlyCollection<TransactionHeaderEntity>> ExcludeExistingInDb(
            DatabaseContext context,
            IReadOnlyCollection<TransactionHeaderEntity> entities)
        {
            var allGlobalIds = entities.Select(t => t.GlobalId);
            var existingIds = await context.TransactionHeaders
                .Where(t => allGlobalIds.Contains(t.GlobalId))
                .Select(t => t.GlobalId)
                .ToDictionaryAsync(x => x);

            return entities.Where(entity => !existingIds.ContainsKey(entity.GlobalId)).ToArray();
        }
    }
}
