using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Blocks;
using Npgsql;

namespace Indexer.Common.Persistence.Entities.BlockHeaders
{
    internal class BlockHeadersRepository : IBlockHeadersRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;
        private readonly string _blockchainId;

        public BlockHeadersRepository(NpgsqlConnection connection, string schema, string blockchainId)
        {
            _connection = connection;
            _schema = schema;
            _blockchainId = blockchainId;
        }

        public async Task InsertOrIgnore(BlockHeader blockHeader)
        {
            var query = @$"
                    insert into {_schema}.{TableNames.BlockHeaders} 
                    (
                        id,
                        number,
                        previous_id,
                        mined_at
                    ) 
                    values 
                    (
                        @id,
                        @number,
                        @previousId,
                        @minedAt
                    ) 
                    on conflict (id) do nothing";

            await _connection.ExecuteAsync(
                query,
                new
                {
                    id = blockHeader.Id,
                    number = blockHeader.Number,
                    previousId = blockHeader.PreviousId,
                    minedAt = blockHeader.MinedAt
                });
        }

        public async Task<BlockHeader> GetOrDefault(long blockNumber)
        {
            var query = $"select * from {_schema}.{TableNames.BlockHeaders} where number = @blockNumber";

            var entity = await _connection.QuerySingleOrDefaultAsync<BlockHeaderEntity>(query, new {blockNumber});

            return entity != null ? MapFromEntity(entity) : null;
        }
        
        public async Task Remove(string id)
        {
            var query = $"delete from {_schema}.{TableNames.BlockHeaders} where id = @id";

            await _connection.ExecuteAsync(query, new {id});
        }

        public async Task<IEnumerable<BlockHeader>> GetBatch(long startBlockNumber, int limit)
        {
            var query = $"select * from {_schema}.{TableNames.BlockHeaders} where number >= @startBlockNumber order by number limit @limit";

            var entities = await _connection.QueryAsync<BlockHeaderEntity>(query, new {startBlockNumber, limit});

            return entities.Select(MapFromEntity);
        }

        public async Task<BlockHeader> GetLast()
        {
            var query = $@"
                select * 
                from {_schema}.{TableNames.BlockHeaders} 
                where number = 
                    (
                        select max(number) 
                        from {_schema}.{TableNames.BlockHeaders}
                    )";

            var entity = await _connection.QuerySingleAsync<BlockHeaderEntity>(query);

            return MapFromEntity(entity);
        }

        private BlockHeader MapFromEntity(BlockHeaderEntity entity)
        {
            return new BlockHeader(
                _blockchainId,
                entity.id,
                entity.number,
                entity.previous_id,
                new DateTime(entity.mined_at.Ticks, DateTimeKind.Utc));
        }
    }
}
