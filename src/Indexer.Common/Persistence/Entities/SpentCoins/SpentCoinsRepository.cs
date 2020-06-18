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
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public SpentCoinsRepository(Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<SpentCoin> coins)
        {
            if (!coins.Any())
            {
                return;
            }
            
            await using var connection = await _connectionFactory.Invoke();
            
            var schema = DbSchema.GetName(blockchainId);
            var copyHelper = new PostgreSQLCopyHelper<SpentCoin>(schema, TableNames.SpentCoins)
                .UsePostgresQuoting()
                .MapVarchar(nameof(SpentCoinEntity.transaction_id), p => p.Id.TransactionId)
                .MapInteger(nameof(SpentCoinEntity.number), p => p.Id.Number)
                .MapVarchar(nameof(SpentCoinEntity.block_id), p => blockId)
                .MapBigInt(nameof(SpentCoinEntity.asset_id), p => p.Unit.AssetId)
                .MapNumeric(nameof(SpentCoinEntity.amount), p => p.Unit.Amount)
                .MapVarchar(nameof(SpentCoinEntity.address), p => p.Address)
                .MapVarchar(nameof(SpentCoinEntity.tag), p => p.Tag)
                .MapNullable(nameof(SpentCoinEntity.tag_type), p => p.TagType, NpgsqlDbType.Integer)
                .MapVarchar(nameof(SpentCoinEntity.spent_by_transaction_id), p => p.SpentByTransactionId)
                .MapInteger(nameof(SpentCoinEntity.spent_by_input_coin_number), p => p.SpentByInputCoinNumber);

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

        private static async Task<IReadOnlyCollection<SpentCoin>> ExcludeExistingInDb(
            string schema,
            NpgsqlConnection connection,
            IReadOnlyCollection<SpentCoin> coins)
        {
            if (!coins.Any())
            {
                return Array.Empty<SpentCoin>();
            }

            var existingEntities = await connection.QueryInList<SpentCoinEntity, SpentCoin>(
                schema,
                TableNames.SpentCoins,
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
