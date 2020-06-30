using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions.Transfers;
using Npgsql;
using NpgsqlTypes;
using PostgreSQLCopyHelper;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.SpentCoins
{
    internal sealed class SpentCoinsRepository : ISpentCoinsRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;

        public SpentCoinsRepository(NpgsqlConnection connection, string schema)
        {
            _connection = connection;
            _schema = schema;
        }

        public async Task InsertOrIgnore(IReadOnlyCollection<SpentCoin> coins)
        {
            if (!coins.Any())
            {
                return;
            }
            
            var copyHelper = new PostgreSQLCopyHelper<SpentCoin>(_schema, TableNames.SpentCoins)
                .UsePostgresQuoting()
                .MapVarchar(nameof(SpentCoinEntity.transaction_id), p => p.Id.TransactionId)
                .MapInteger(nameof(SpentCoinEntity.number), p => p.Id.Number)
                .MapBigInt(nameof(SpentCoinEntity.asset_id), p => p.Unit.AssetId)
                .MapNumeric(nameof(SpentCoinEntity.amount), p => p.Unit.Amount)
                .MapVarchar(nameof(SpentCoinEntity.address), p => p.Address)
                .MapVarchar(nameof(SpentCoinEntity.tag), p => p.Tag)
                .MapNullable(nameof(SpentCoinEntity.tag_type), p => p.TagType, NpgsqlDbType.Integer)
                .MapVarchar(nameof(SpentCoinEntity.spent_by_transaction_id), p => p.SpentByCoinId.TransactionId)
                .MapInteger(nameof(SpentCoinEntity.spent_by_input_coin_number), p => p.SpentByCoinId.Number);

            try
            {
                await copyHelper.SaveAllAsync(_connection, coins);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(coins);

                if (notExisted.Any())
                {
                    await copyHelper.SaveAllAsync(_connection, notExisted);
                }
            }
        }

        public async Task<IReadOnlyCollection<SpentCoin>> GetSpentByBlock(string blockId)
        {
            var query = $@"
                select c.* 
                from {_schema}.{TableNames.SpentCoins} c
                join {_schema}.{TableNames.TransactionHeaders} t on t.id = c.spent_by_transaction_id
                where t.block_id = @blockId";

            var entities = await _connection.QueryAsync<SpentCoinEntity>(query, new {blockId});

            return entities
                .Select(x => new SpentCoin(
                    new CoinId(x.transaction_id, x.number),
                    new Unit(x.asset_id, x.amount),
                    x.address,
                    x.tag,
                    (DestinationTagType?) x.tag_type,
                    new CoinId(x.spent_by_transaction_id, x.spent_by_input_coin_number)))
                .ToArray();
        }

        public async Task RemoveSpentByBlock(string blockId)
        {
            var query = $@"
                delete 
                from {_schema}.{TableNames.SpentCoins} c
                using {_schema}.{TableNames.TransactionHeaders} t
                where 
                    t.id = c.spent_by_transaction_id and
                    t.block_id = @blockId";

            await _connection.ExecuteAsync(query, new {blockId});
        }

        private async Task<IReadOnlyCollection<SpentCoin>> ExcludeExistingInDb(IReadOnlyCollection<SpentCoin> coins)
        {
            if (!coins.Any())
            {
                return Array.Empty<SpentCoin>();
            }

            var existingEntities = await _connection.QueryInList<SpentCoinEntity, SpentCoin>(
                _schema,
                TableNames.SpentCoins,
                coins,
                columnsToSelect: "transaction_id, number ",
                listColumns: "transaction_id, number",
                x => $"'{x.Id.TransactionId}', {x.Id.Number}",
                knownSourceLength: coins.Count);

            var existing = existingEntities
                .Select(x => new CoinId(x.transaction_id, x.number))
                .ToHashSet();

            return coins.Where(x => !existing.Contains(x.Id)).ToArray();
        }
    }
}
