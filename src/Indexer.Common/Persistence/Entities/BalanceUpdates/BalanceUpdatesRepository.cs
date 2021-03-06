﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions.Transfers;
using Npgsql;
using PostgreSQLCopyHelper;
using Swisschain.Extensions.Postgres;

namespace Indexer.Common.Persistence.Entities.BalanceUpdates
{
    internal sealed class BalanceUpdatesRepository : IBalanceUpdatesRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;

        public BalanceUpdatesRepository(NpgsqlConnection connection, string schema)
        {
            _connection = connection;
            _schema = schema;
        }

        /// <summary>
        /// Only one item for the (address, assetId, blockNumber) tuple should be in the list.
        /// Only balance updates from the same block should be in the list.
        /// It's not checked due to performance reason
        /// </summary>
        public async Task InsertOrIgnore(IReadOnlyCollection<BalanceUpdate> balanceUpdates)
        {
            if (!balanceUpdates.Any())
            {
                return;
            }
            
            var copyHelper = new PostgreSQLCopyHelper<BalanceUpdate>(_schema, TableNames.BalanceUpdates)
                .UsePostgresQuoting()
                .MapVarchar(nameof(BalanceUpdateEntity.address), x => x.Address)
                .MapBigInt(nameof(BalanceUpdateEntity.asset_id), x => x.AssetId)
                .MapVarchar(nameof(BalanceUpdateEntity.block_id), x => x.BlockId)
                .MapBigInt(nameof(BalanceUpdateEntity.block_number), x => x.BlockNumber)
                .MapTimeStamp(nameof(BalanceUpdateEntity.block_mined_at), x => x.BlockMinedAt)
                .MapNumeric(nameof(BalanceUpdateEntity.amount), x => x.Amount);

            try
            {
                await copyHelper.SaveAllAsync(_connection, balanceUpdates);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(balanceUpdates);

                if (notExisted.Any())
                {
                    await copyHelper.SaveAllAsync(_connection, notExisted);
                }
            }
        }

        public async Task RemoveByBlock(string blockId)
        {
            var query = $@"delete from {_schema}.{TableNames.BalanceUpdates} where block_id = @blockId";

            await _connection.ExecuteAsync(query, new {blockId});
        }

        // TODO: to get a balance at a specified block number
        // select sum(amount) from bitcoin.balance_updates where address='1VayNert3x1KzbpzMGt2qdqrAThiRovi8' and block_number<=232985

        private async Task<IReadOnlyCollection<BalanceUpdate>> ExcludeExistingInDb(IReadOnlyCollection<BalanceUpdate> balanceUpdates)
        {
            if (!balanceUpdates.Any())
            {
                return Array.Empty<BalanceUpdate>();
            }

            var existingEntities = await _connection.QueryInList<BalanceUpdateEntity, BalanceUpdate>(
                _schema,
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
