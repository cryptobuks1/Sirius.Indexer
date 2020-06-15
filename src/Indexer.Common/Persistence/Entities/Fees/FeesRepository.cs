﻿using System;
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
                .MapBigInt(nameof(FeeEntity.asset_id), x => x.AssetId)
                .MapVarchar(nameof(FeeEntity.block_id), x => x.BlockId)
                .MapNumeric(nameof(FeeEntity.amount), x => x.Amount);

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

        private async Task<IReadOnlyCollection<Fee>> ExcludeExistingInDb(string schema, NpgsqlConnection connection, IReadOnlyCollection<Fee> fees)
        {
            if (!fees.Any())
            {
                return Array.Empty<Fee>();
            }

            var inList = string.Join(", ", fees.Select(x => $"('{x.TransactionId}', {x.AssetId})"));
            
            // limit is specified to avoid scanning indexes of the partitions once all headers are found
            var query = $"select transaction_id, asset_id from {schema}.{TableNames.Fees} where (transaction_id, asset_id) in ({inList}) limit @limit";
            var existingEntities = await connection.QueryAsync<FeeEntity>(query, new {limit = fees.Count});

            var existing = existingEntities
                .Select(x => (x.transaction_id, x.asset_id))
                .ToHashSet();

            return fees.Where(x => !existing.Contains((x.TransactionId, x.AssetId))).ToArray();
        }
    }
}