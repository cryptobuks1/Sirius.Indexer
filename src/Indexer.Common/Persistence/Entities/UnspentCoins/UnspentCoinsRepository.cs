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
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;

        public UnspentCoinsRepository(NpgsqlConnection connection, string schema)
        {
            _connection = connection;
            _schema = schema;
        }

        public async Task InsertOrIgnore(IReadOnlyCollection<UnspentCoin> coins)
        {
            if (!coins.Any())
            {
                return;
            }
            
            var copyHelper = new PostgreSQLCopyHelper<UnspentCoin>(_schema, TableNames.UnspentCoins)
                .UsePostgresQuoting()
                .MapVarchar(nameof(UnspentCoinEntity.transaction_id), p => p.Id.TransactionId)
                .MapInteger(nameof(UnspentCoinEntity.number), p => p.Id.Number)
                .MapBigInt(nameof(UnspentCoinEntity.asset_id), p => p.Unit.AssetId)
                .MapNumeric(nameof(UnspentCoinEntity.amount), p => p.Unit.Amount)
                .MapVarchar(nameof(UnspentCoinEntity.address), p => p.Address)
                .MapVarchar(nameof(UnspentCoinEntity.script_pub_key), p => p.ScriptPubKey)
                .MapVarchar(nameof(UnspentCoinEntity.tag), p => p.Tag)
                .MapNullable(nameof(UnspentCoinEntity.tag_type), p => p.TagType, NpgsqlDbType.Integer);

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

        public async Task<IReadOnlyCollection<UnspentCoin>> GetAnyOf(IReadOnlyCollection<CoinId> ids)
        {
            if (!ids.Any())
            {
                return Array.Empty<UnspentCoin>();
            }
            
            var entities = await _connection.QueryInList<UnspentCoinEntity, CoinId>(
                _schema,
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

        public async Task Remove(IReadOnlyCollection<CoinId> ids)
        {
            if (!ids.Any())
            {
                return;
            }
            
            async Task RemoveBatch(IEnumerable<CoinId> batch)
            {
                var inList = string.Join(", ", batch.Select(x => $"('{x.TransactionId}', {x.Number})"));
                var query = $"delete from {_schema}.{TableNames.UnspentCoins} where (transaction_id, number) in (values {inList})";

                await _connection.ExecuteAsync(query);
            }

            foreach (var batch in MoreLinq.MoreEnumerable.Batch(ids, 1000))
            {
                await RemoveBatch(batch);
            }
        }

        public async Task<IReadOnlyCollection<UnspentCoin>> GetByBlock(string blockId)
        {
            var query = $@"
                select c.* 
                from {_schema}.{TableNames.UnspentCoins} c
                join {_schema}.{TableNames.TransactionHeaders} t on t.id = c.transaction_id
                where t.block_id = @blockId";

            var entities = await _connection.QueryAsync<UnspentCoinEntity>(query, new {blockId});

            return entities
                .Select(MapToDomain)
                .ToArray();
        }

        public async Task<IReadOnlyCollection<UnspentCoin>> GetByAddress(string address, long? asAtBlockNumber)
        {
            var query = asAtBlockNumber.HasValue
                ? $@"
                    select c.*
                    from {_schema}.{TableNames.UnspentCoins} c
                    join {_schema}.{TableNames.TransactionHeaders} t on t.id = c.transaction_id
                    join {_schema}.{TableNames.BlockHeaders} b on b.id = t.block_id
                    where 
                        c.address = @address and
                        b.number <= @asAtBlockNumber"
                : $@"
                    select c.*
                    from {_schema}.{TableNames.UnspentCoins} c
                    where c.address = @address";

            var entities = await _connection.QueryAsync<UnspentCoinEntity>(query, new {address, asAtBlockNumber});

            return entities
                .Select(MapToDomain)
                .ToArray();
        }

        public async Task RemoveByBlock(string blockId)
        {
            var query = $@"
                delete 
                from {_schema}.{TableNames.UnspentCoins} c
                using {_schema}.{TableNames.TransactionHeaders} t
                where 
                    t.id = c.transaction_id and
                    t.block_id = @blockId";

            await _connection.ExecuteAsync(query, new {blockId});
        }

        private async Task<IReadOnlyCollection<UnspentCoin>> ExcludeExistingInDb(IReadOnlyCollection<UnspentCoin> coins)
        {
            if (!coins.Any())
            {
                return Array.Empty<UnspentCoin>();
            }

            var existingEntities = await _connection.QueryInList<UnspentCoinEntity, UnspentCoin>(
                _schema,
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
                entity.script_pub_key,
                entity.tag,
                (DestinationTagType?) entity.tag_type);
        }
    }
}
