using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
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

        public async Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<CoinId> coins)
        {
            if (!coins.Any())
            {
                return;
            }
            
            await using var connection = await _connectionFactory.Invoke();
            
            var schema = DbSchema.GetName(blockchainId);
            var copyHelper = new PostgreSQLCopyHelper<CoinId>(schema, TableNames.InputCoins)
                .UsePostgresQuoting()
                .MapVarchar(nameof(InputCoinEntity.transaction_id), p => p.TransactionId)
                .MapInteger(nameof(InputCoinEntity.number), p => p.Number)
                .MapVarchar(nameof(InputCoinEntity.block_id), p => blockId);

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

        private static async Task<IReadOnlyCollection<CoinId>> ExcludeExistingInDb(
            string schema,
            NpgsqlConnection connection,
            IReadOnlyCollection<CoinId> coins)
        {
            if (!coins.Any())
            {
                return Array.Empty<CoinId>();
            }

            var inList = string.Join(", ", coins.Select(x => $"('{x.TransactionId}', {x.Number})"));
            
            // limit is specified to avoid scanning indexes of the partitions once all headers are found
            var query = $"select id from {schema}.{TableNames.InputCoins} where (transaction_id, number) in ('{inList}') limit @limit";
            var existingEntities = await connection.QueryAsync<InputCoinEntity>(query, new {limit = coins.Count});

            var existing = existingEntities
                .Select(x => new CoinId(x.transaction_id, x.number))
                .ToHashSet();
            
            return coins.Where(x => !existing.Contains(x)).ToArray();
        }
    }
}
