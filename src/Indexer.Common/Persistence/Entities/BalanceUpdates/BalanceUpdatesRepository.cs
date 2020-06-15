﻿using System;
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
                .MapNumeric(nameof(BalanceUpdateEntity.amount), x => x.Amount)
                .MapNumeric(nameof(BalanceUpdateEntity.total), x => x.Total);

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

            await UpdateTotalBalance(connection, schema, balanceUpdates);
        }

        private static Task UpdateTotalBalance(NpgsqlConnection connection, 
            string schema,
            IReadOnlyCollection<BalanceUpdate> balanceUpdates)
        {
            var inList = string.Join(", ", balanceUpdates.Select(x => $"('{x.Address}', {x.AssetId}, {x.BlockNumber})"));

            var query = $@"
                update {schema}.{TableNames.BalanceUpdates} as cur
	            set total = amount + 
	            (
		            select prev_total from coalesce 
		            (
			            (
                            select total from {schema}.{TableNames.BalanceUpdates} as prev
				            where 
			 		            prev.address = cur.address and
			 		            prev.asset_id = cur.asset_id and
			 		            prev.block_number < cur.block_number
				            order by prev.block_number desc
				            limit 1
                        ),
			            0
		            ) as prev_total
	            )
	            where (address, asset_id, block_number) in 
	            (
		            {inList}
	            )";

            return connection.ExecuteAsync(query);
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

            var inList = string.Join(", ", balanceUpdates.Select(x => $"('{x.Address}', {x.AssetId}, {x.BlockNumber})"));
            
            // limit is specified to avoid scanning indexes of the partitions once all headers are found
            var query = $"select address, asset_id, block_number from {schema}.{TableNames.BalanceUpdates} where (address, asset_id, block_number) in ({inList}) limit @limit";
            var existingEntities = await connection.QueryAsync<BalanceUpdate>(query, new {limit = balanceUpdates.Count});

            var existing = existingEntities
                .Select(x => (x.Address, x.AssetId, x.BlockNumber))
                .ToHashSet();

            return balanceUpdates.Where(x => !existing.Contains((x.Address, x.AssetId, x.BlockNumber))).ToArray();
        }
    }
}
