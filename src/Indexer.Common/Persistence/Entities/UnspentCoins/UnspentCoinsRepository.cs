﻿using System;
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

        public async Task InsertOrIgnore(string blockchainId, IReadOnlyCollection<UnspentCoin> coins)
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

        public async Task<IReadOnlyCollection<UnspentCoin>> GetAnyOf(string blockchainId, IReadOnlyCollection<CoinId> ids)
        {
            if (!ids.Any())
            {
                return Array.Empty<UnspentCoin>();
            }
            
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);
            var entities = await connection.QueryInList<UnspentCoinEntity, CoinId>(
                schema,
                TableNames.UnspentCoins,
                ids,
                columnsToSelect: "*",
                listColumns: "transaction_id, number",
                x => $"'{x.TransactionId}', {x.Number}",
                knownSourceLength: ids.Count);

            var domainObjects = entities
                .Select(MapToDomain)
                .ToArray();

            return domainObjects;
        }

        public async Task Remove(string blockchainId, IReadOnlyCollection<CoinId> ids)
        {
            if (!ids.Any())
            {
                return;
            }
            
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);

            async Task RemoveBatch(NpgsqlConnection conn, IEnumerable<CoinId> batch)
            {
                var inList = string.Join(", ", batch.Select(x => $"('{x.TransactionId}', {x.Number})"));
                var query = $"delete from {schema}.{TableNames.UnspentCoins} where (transaction_id, number) in (values {inList})";

                await conn.ExecuteAsync(query);
            }

            foreach (var batch in MoreLinq.MoreEnumerable.Batch(ids, 1000))
            {
                await RemoveBatch(connection, batch);
            }
        }

        public async Task<IReadOnlyCollection<UnspentCoin>> GetByBlock(string blockchainId, string blockId)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);
            var query = $@"
                select c.* 
                from {schema}.{TableNames.UnspentCoins} c
                join {schema}.{TableNames.TransactionHeaders} t on t.id = c.transaction_id
                where t.block_id = @blockId";

            var entities = await connection.QueryAsync<UnspentCoinEntity>(query, new {blockId});

            return entities
                .Select(MapToDomain)
                .ToArray();
        }

        public async Task RemoveByBlock(string blockchainId, string blockId)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = DbSchema.GetName(blockchainId);
            var query = $@"
                delete 
                from {schema}.{TableNames.UnspentCoins} c
                using {schema}.{TableNames.TransactionHeaders} t
                where 
                    t.id = c.transaction_id and
                    t.block_id = @blockId";

            await connection.ExecuteAsync(query, new {blockId});
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

            var existingEntities = await connection.QueryInList<UnspentCoinEntity, UnspentCoin>(
                schema,
                TableNames.UnspentCoins,
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

        private static UnspentCoin MapToDomain(UnspentCoinEntity entity)
        {
            return new UnspentCoin(
                new CoinId(entity.transaction_id, entity.number),
                new Unit(entity.asset_id, entity.amount),
                entity.address,
                entity.tag,
                (DestinationTagType?) entity.tag_type);
        }
    }
}
