using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Npgsql;
using PostgreSQLCopyHelper;

namespace Indexer.Common.Persistence.Entities.NonceUpdates
{
    internal sealed class NonceUpdatesRepository : INonceUpdatesRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;

        public NonceUpdatesRepository(NpgsqlConnection connection, string schema)
        {
            _connection = connection;
            _schema = schema;
        }

        public async Task InsertOrIgnore(IReadOnlyCollection<NonceUpdate> nonceUpdates)
        {
            if (!nonceUpdates.Any())
            {
                return;
            }

            var copyHelper = new PostgreSQLCopyHelper<NonceUpdate>(_schema, TableNames.NonceUpdates)
                .UsePostgresQuoting()
                .MapVarchar(nameof(NonceUpdateEntity.address), x => x.Address)
                .MapVarchar(nameof(NonceUpdateEntity.block_id), x => x.BlockId)
                .MapBigInt(nameof(NonceUpdateEntity.nonce), x => x.Nonce);

            try
            {
                await copyHelper.SaveAllAsync(_connection, nonceUpdates);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(nonceUpdates);

                if (notExisted.Any())
                {
                    await copyHelper.SaveAllAsync(_connection, notExisted);
                }
            }
        }

        public async Task<NonceUpdate> GetLatestOrDefault(string address, long? asAtBlockNumber)
        {
            var query = asAtBlockNumber.HasValue
                ? $@"
                    select n.*
                    from {_schema}.{TableNames.NonceUpdates} n
                    join {_schema}.{TableNames.BlockHeaders} b on b.id = n.block_id
                    where 
                        n.address = @address and
                        b.number <= @asAtBlockNumber
                    order by b.number desc limit 1"
                : $@"
                    select n.*
                    from {_schema}.{TableNames.NonceUpdates} n
                    join {_schema}.{TableNames.BlockHeaders} b on b.id = n.block_id
                    where 
                        n.address = @address
                    order by b.number desc limit 1";

            var entity = await _connection.QuerySingleOrDefaultAsync<NonceUpdateEntity>(query, new {address, asAtBlockNumber});

            return entity != null ? MapToDomain(entity) : null;
        }

        public async Task RemoveByBlock(string blockId)
        {
            var query = $"delete from {_schema}.{TableNames.NonceUpdates} where block_id = @blockId";

            await _connection.ExecuteAsync(query, new {blockId});
        }

        private static NonceUpdate MapToDomain(NonceUpdateEntity entity)
        {
            return new NonceUpdate(entity.address, entity.block_id, entity.nonce);
        }

        private async Task<IReadOnlyCollection<NonceUpdate>> ExcludeExistingInDb(IReadOnlyCollection<NonceUpdate> nonceUpdates)
        {
            if (!nonceUpdates.Any())
            {
                return Array.Empty<NonceUpdate>();
            }
            
            var existingEntities = await _connection.QueryInList<NonceUpdateEntity, NonceUpdate>(
                _schema,
                TableNames.NonceUpdates,
                nonceUpdates,
                columnsToSelect: "address, block_id",
                listColumns: "address, block_id",
                x => $"'{x.Address}', '{x.BlockId}'",
                knownSourceLength: nonceUpdates.Count);

            var existing = existingEntities
                .Select(x => (x.address, x.block_id))
                .ToHashSet();

            return nonceUpdates.Where(x => !existing.Contains((x.Address, x.BlockId))).ToArray();
        }
    }
}
