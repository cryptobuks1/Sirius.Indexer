using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions;
using Npgsql;
using PostgreSQLCopyHelper;

namespace Indexer.Common.Persistence.Entities.Fees
{
    internal sealed class FeesRepository : IFeesRepository
    {
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public FeesRepository(Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InsertOrIgnore(string blockchainId, IReadOnlyCollection<Fee> fees)
        {
            if (!fees.Any())
            {
                return;
            }
            
            await using var connection = await _connectionFactory.Invoke();
            
            var schema = DbSchema.GetName(blockchainId);
            var copyHelper = new PostgreSQLCopyHelper<Fee>(schema, TableNames.Fees)
                .UsePostgresQuoting()
                .MapVarchar(nameof(FeeEntity.transaction_id), x => x.TransactionId)
                .MapBigInt(nameof(FeeEntity.asset_id), x => x.Unit.AssetId)
                .MapNumeric(nameof(FeeEntity.amount), x => x.Unit.Amount);

            try
            {
                await copyHelper.SaveAllAsync(connection, fees);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(schema, connection, fees);

                if (notExisted.Any())
                {
                    await copyHelper.SaveAllAsync(connection, notExisted);
                }
            }

        }

        public async Task RemoveByBlock(string blockchainId, string blockId)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);
            var query = $@"
                delete 
                from {schema}.{TableNames.Fees} f
                using {schema}.{TableNames.TransactionHeaders} t
                where 
                    t.id = f.transaction_id and
                    t.block_id = @blockId";

            await connection.ExecuteAsync(query, new {blockId});
        }

        private static async Task<IReadOnlyCollection<Fee>> ExcludeExistingInDb(string schema, NpgsqlConnection connection, IReadOnlyCollection<Fee> fees)
        {
            if (!fees.Any())
            {
                return Array.Empty<Fee>();
            }

            var existingEntities = await connection.QueryInList<FeeEntity, Fee>(
                schema,
                TableNames.Fees,
                fees,
                columnsToSelect: "transaction_id, asset_id",
                listColumns: "transaction_id, asset_id",
                x => $"'{x.TransactionId}', {x.Unit.AssetId}",
                knownSourceLength: fees.Count);

            var existing = existingEntities
                .Select(x => (x.transaction_id, x.asset_id))
                .ToHashSet();

            return fees.Where(x => !existing.Contains((x.TransactionId, x.Unit.AssetId))).ToArray();
        }
    }
}
