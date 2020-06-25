using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions.Transfers;
using Npgsql;
using PostgreSQLCopyHelper;

namespace Indexer.Common.Persistence.Entities.BalanceUpdates
{
    internal sealed class BalanceUpdatesRepository : IBalanceUpdatesRepository
    {
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public BalanceUpdatesRepository(Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Only one item for the (address, assetId, blockNumber) tuple should be in the list.
        /// Only balance updates from the same block should be in the list.
        /// It's not checked due to performance reason
        /// </summary>
        public async Task InsertOrIgnore(string blockchainId, IReadOnlyCollection<BalanceUpdate> balanceUpdates)
        {
            if (!balanceUpdates.Any())
            {
                return;
            }
            
            await using var connection = await _connectionFactory.Invoke();
            
            var schema = DbSchema.GetName(blockchainId);
            var copyHelper = new PostgreSQLCopyHelper<BalanceUpdate>(schema, TableNames.BalanceUpdates)
                .UsePostgresQuoting()
                .MapVarchar(nameof(BalanceUpdateEntity.address), x => x.Address)
                .MapBigInt(nameof(BalanceUpdateEntity.asset_id), x => x.AssetId)
                .MapVarchar(nameof(BalanceUpdateEntity.block_id), x => x.BlockId)
                .MapBigInt(nameof(BalanceUpdateEntity.block_number), x => x.BlockNumber)
                .MapTimeStamp(nameof(BalanceUpdateEntity.block_mined_at), x => x.BlockMinedAt)
                .MapNumeric(nameof(BalanceUpdateEntity.amount), x => x.Amount);

            try
            {
                await copyHelper.SaveAllAsync(connection, balanceUpdates);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(schema, connection, balanceUpdates);

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
            var query = $@"delete from {schema}.{TableNames.BalanceUpdates} where block_id = @blockId";

            await connection.ExecuteAsync(query, new {blockId});
        }

        private static async Task<IReadOnlyCollection<BalanceUpdate>> ExcludeExistingInDb(
            string schema,
            NpgsqlConnection connection,
            IReadOnlyCollection<BalanceUpdate> balanceUpdates)
        {
            if (!balanceUpdates.Any())
            {
                return Array.Empty<BalanceUpdate>();
            }

            var existingEntities = await connection.QueryInList<BalanceUpdateEntity, BalanceUpdate>(
                schema,
                TableNames.BalanceUpdates,
                balanceUpdates,
                columnsToSelect: "address, asset_id, block_number",
                listColumns: "address, asset_id, block_number",
                x => $"'{x.Address}', {x.AssetId}, {x.BlockNumber}",
                knownSourceLength: balanceUpdates.Count);

            var existing = existingEntities
                .Select(x => (x.address, x.asset_id, x.block_number))
                .ToHashSet();

            return balanceUpdates.Where(x => !existing.Contains((x.Address, x.AssetId, x.BlockNumber))).ToArray();
        }
    }
}
