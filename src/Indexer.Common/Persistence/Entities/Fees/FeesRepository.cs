using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions;
using Npgsql;
using PostgreSQLCopyHelper;
using Swisschain.Extensions.Postgres;

namespace Indexer.Common.Persistence.Entities.Fees
{
    internal sealed class FeesRepository : IFeesRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;

        public FeesRepository(NpgsqlConnection connection, string schema)
        {
            _connection = connection;
            _schema = schema;
        }

        public async Task InsertOrIgnore(IReadOnlyCollection<Fee> fees)
        {
            if (!fees.Any())
            {
                return;
            }
            
            var copyHelper = new PostgreSQLCopyHelper<Fee>(_schema, TableNames.Fees)
                .UsePostgresQuoting()
                .MapVarchar(nameof(FeeEntity.transaction_id), x => x.TransactionId)
                .MapBigInt(nameof(FeeEntity.asset_id), x => x.Unit.AssetId)
                .MapNumeric(nameof(FeeEntity.amount), x => x.Unit.Amount);

            try
            {
                await copyHelper.SaveAllAsync(_connection, fees);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(fees);

                if (notExisted.Any())
                {
                    await copyHelper.SaveAllAsync(_connection, notExisted);
                }
            }

        }

        public async Task RemoveByBlock(string blockId)
        {
            var query = $@"
                delete 
                from {_schema}.{TableNames.Fees} f
                using {_schema}.{TableNames.TransactionHeaders} t
                where 
                    t.id = f.transaction_id and
                    t.block_id = @blockId";

            await _connection.ExecuteAsync(query, new {blockId});
        }

        private async Task<IReadOnlyCollection<Fee>> ExcludeExistingInDb(IReadOnlyCollection<Fee> fees)
        {
            if (!fees.Any())
            {
                return Array.Empty<Fee>();
            }

            var existingEntities = await _connection.QueryInList<FeeEntity, Fee>(
                _schema,
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
