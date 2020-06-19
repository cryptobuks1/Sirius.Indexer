using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions.Transfers;
using Npgsql;
using NpgsqlTypes;
using Swisschain.Sirius.Sdk.Primitives;
using PostgreSQLCopyHelper;

namespace Indexer.Common.Persistence.Entities.InputCoins
{
    internal sealed class InputCoinsRepository : IInputCoinsRepository
    {
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public InputCoinsRepository(Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<InputCoin> coins)
        {
            if (!coins.Any())
            {
                return;
            }
            
            await using var connection = await _connectionFactory.Invoke();
            
            var schema = DbSchema.GetName(blockchainId);
            var copyHelper = new PostgreSQLCopyHelper<InputCoin>(schema, TableNames.InputCoins)
                .UsePostgresQuoting()
                .MapVarchar(nameof(InputCoinEntity.transaction_id), x => x.Id.TransactionId)
                .MapInteger(nameof(InputCoinEntity.number), x => x.Id.Number)
                .MapInteger(nameof(InputCoinEntity.type), x => (int)x.Type)
                .MapVarchar(nameof(InputCoinEntity.prev_output_transaction_id), x => x.PreviousOutput?.TransactionId)
                .MapNullable(nameof(InputCoinEntity.prev_output_coin_number), x => x.PreviousOutput?.Number, NpgsqlDbType.Integer);

            try
            {
                await copyHelper.SaveAllAsync(connection, coins);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(schema, connection, coins);

                if (notExisted.Any())
                {
                    await copyHelper.SaveAllAsync(connection, notExisted);
                }
            }
        }

        public async Task<IReadOnlyCollection<InputCoin>> GetByBlock(string blockchainId, string blockId)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);
            var query = $@"
                select * 
                from {schema}.{TableNames.InputCoins} c
                join {schema}.{TableNames.TransactionHeaders} t on t.id = c.transaction_id
                where t.block_id = @blockId";

            var entities = await connection.QueryAsync<InputCoinEntity>(query, new {blockId});

            return entities
                .Select(x => new InputCoin(
                    new CoinId(x.transaction_id, x.number),
                    (InputCoinType) x.type,
                    x.prev_output_transaction_id != null && x.prev_output_coin_number != null
                        ? new CoinId(x.prev_output_transaction_id, x.prev_output_coin_number.Value)
                        : null))
                .ToArray();
        }

        public async Task RemoveByBlock(string blockchainId, string blockId)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);
            var query = $@"
                delete 
                from {schema}.{TableNames.InputCoins} c
                using {schema}.{TableNames.TransactionHeaders} t
                where 
                    t.id = c.transaction_id and
                    t.block_id = @blockId";

            await connection.ExecuteAsync(query, new {blockId});
        }

        private static async Task<IReadOnlyCollection<InputCoin>> ExcludeExistingInDb(
            string schema,
            NpgsqlConnection connection,
            IReadOnlyCollection<InputCoin> coins)
        {
            if (!coins.Any())
            {
                return Array.Empty<InputCoin>();
            }
            
            var existingEntities = await connection.QueryInList<InputCoinEntity, InputCoin>(
                schema,
                TableNames.InputCoins,
                coins,
                columnsToSelect: "transaction_id, number ",
                listColumns: "transaction_id, number",
                x => $"('{x.Id.TransactionId}', {x.Id.Number})",
                knownSourceLength: coins.Count);

            var existing = existingEntities
                .Select(x => new CoinId(x.transaction_id, x.number))
                .ToHashSet();
            
            return coins.Where(x => !existing.Contains(x.Id)).ToArray();
        }
    }
}
