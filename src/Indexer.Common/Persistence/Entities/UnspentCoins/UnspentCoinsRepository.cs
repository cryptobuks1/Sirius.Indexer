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

namespace Indexer.Common.Persistence.Entities.UnspentCoins
{
    internal sealed class UnspentCoinsRepository : IUnspentCoinsRepository
    {
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public UnspentCoinsRepository(Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<UnspentCoin> coins)
        {
            if (!coins.Any())
            {
                return;
            }
            
            await using var connection = await _connectionFactory.Invoke();
            
            var schema = DbSchema.GetName(blockchainId);
            var copyHelper = new PostgreSQLCopyHelper<UnspentCoin>(schema, TableNames.UnspentCoins)
                .UsePostgresQuoting()
                .MapVarchar(nameof(UnspentCoinEntity.transaction_id), p => p.Id.TransactionId)
                .MapInteger(nameof(UnspentCoinEntity.number), p => p.Id.Number)
                .MapVarchar(nameof(UnspentCoinEntity.block_id), p => blockId)
                .MapBigInt(nameof(UnspentCoinEntity.asset_id), p => p.Unit.AssetId)
                .MapNumeric(nameof(UnspentCoinEntity.amount), p => p.Unit.Amount)
                .MapVarchar(nameof(UnspentCoinEntity.address), p => p.Address)
                .MapVarchar(nameof(UnspentCoinEntity.tag), p => p.Tag)
                .MapNullable(nameof(UnspentCoinEntity.tag_type), p => p.TagType, NpgsqlDbType.Integer);

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

        private static async Task<IReadOnlyCollection<UnspentCoin>> ExcludeExistingInDb(
            string schema,
            NpgsqlConnection connection,
            IReadOnlyCollection<UnspentCoin> coins)
        {
            if (!coins.Any())
            {
                return Array.Empty<UnspentCoin>();
            }

            var inList = string.Join(", ", coins.Select(x => $"('{x.Id.TransactionId}', {x.Id.Number})"));
            
            // limit is specified to avoid scanning indexes of the partitions once all headers are found
            var query = $"select transaction_id, number from {schema}.{TableNames.UnspentCoins} where (transaction_id, number) in ({inList}) limit @limit";
            var existingEntities = await connection.QueryAsync<UnspentCoinEntity>(query, new {limit = coins.Count});

            var existing = existingEntities
                .Select(x => new CoinId(x.transaction_id, x.number))
                .ToHashSet();

            return coins.Where(x => !existing.Contains(x.Id)).ToArray();
        }
    }
}
