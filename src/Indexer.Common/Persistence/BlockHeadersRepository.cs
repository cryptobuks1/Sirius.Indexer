using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Persistence.Entities;
using Indexer.Common.Persistence.EntityFramework;
using Indexer.Common.Telemetry;
using Npgsql;

namespace Indexer.Common.Persistence
{
    internal class BlockHeadersRepository : IBlockHeadersRepository
    {
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;
        private readonly IAppInsight _appInsight;

        public BlockHeadersRepository(Func<Task<NpgsqlConnection>> connectionFactory, IAppInsight appInsight)
        {
            _connectionFactory = connectionFactory;
            _appInsight = appInsight;
        }

        public async Task InsertOrIgnore(BlockHeader blockHeader)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = BlockchainSchema.Get(blockHeader.BlockchainId);
            var query = @$"
                    insert into {schema}.{TableNames.BlockHeaders} 
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

            var telemetry = _appInsight.StartSqlCommand(query);

            try
            {
                await connection.ExecuteAsync(
                    query,
                    new
                    {
                        id = blockHeader.Id,
                        number = blockHeader.Number,
                        previousId = blockHeader.PreviousId,
                        minedAt = blockHeader.MinedAt
                    });

                telemetry.Complete();
            }
            catch (Exception ex)
            {
                telemetry.Fail(ex);

                throw;
            }
        }

        public async Task<BlockHeader> GetOrDefault(string blockchainId, long blockNumber)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = BlockchainSchema.Get(blockchainId);
            var query = $"select * from {schema}.{TableNames.BlockHeaders} where number = @blockNumber";

            var entity = await connection.QuerySingleOrDefaultAsync<BlockHeaderEntity>(query, new {blockNumber});

            return entity != null ? MapFromEntity(blockchainId, entity) : null;
        }
        
        public async Task Remove(string blockchainId, string id)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = BlockchainSchema.Get(blockchainId);
            var query = $"delete from {schema}.{TableNames.BlockHeaders} where id = @id";

            await connection.ExecuteAsync(query, new {id});
        }

        public async Task<IEnumerable<BlockHeader>> GetBatch(string blockchainId, long startBlockNumber, int limit)
        {
            await using var connection = await _connectionFactory.Invoke();

            var schema = BlockchainSchema.Get(blockchainId);
            var query = $"select * from {schema}.{TableNames.BlockHeaders} where number >= @startBlockNumber order by number limit @limit";

            var entities = await connection.QueryAsync<BlockHeaderEntity>(query, new {startBlockNumber, limit});

            return entities.Select(x => MapFromEntity(blockchainId, x));
        }

        private static BlockHeader MapFromEntity(string blockchainId, BlockHeaderEntity entity)
        {
            return new BlockHeader(
                blockchainId,
                entity.id,
                entity.number,
                entity.previous_id,
                new DateTime(entity.mined_at.Ticks, DateTimeKind.Utc));
        }
    }
}
