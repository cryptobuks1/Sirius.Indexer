using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Z.EntityFramework.Plus;

namespace Indexer.Common.Persistence
{
    internal class BlockHeadersRepository : IBlockHeadersRepository
    {
        private readonly Func<DatabaseContext> _contextFactory;

        public BlockHeadersRepository(Func<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task InsertOrIgnore(BlockHeader blockHeader)
        {
            await using var context = _contextFactory.Invoke();
            await using var connection = context.Database.GetDbConnection();
            
            await using var command = connection.CreateCommand();

            var telemetry = context.AppInsight.StartSqlCommand(command);

            try
            {
                if (command.Connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                command.CommandText = @$"
insert into {DatabaseContext.SchemaName}.{TableNames.BlockHeaders} 
(
    ""GlobalId"",
    ""BlockchainId"",
    ""Id"",
    ""Number"",
    ""PreviousId"",
    ""MinedAt""
) 
values 
(
    @globalId,
    @blockchainId,
    @id,
    @number,
    @previousId,
    @minedAt
) 
on conflict (""GlobalId"") do nothing";

                command.Parameters.Add(new NpgsqlParameter("@globalId", DbType.String) {Value = blockHeader.GlobalId});
                command.Parameters.Add(new NpgsqlParameter("@blockchainId", DbType.String) {Value = blockHeader.BlockchainId});
                command.Parameters.Add(new NpgsqlParameter("@id", DbType.String) {Value = blockHeader.Id});
                command.Parameters.Add(new NpgsqlParameter("@number", DbType.Int64) {Value = blockHeader.Number});
                command.Parameters.Add(new NpgsqlParameter("@previousId", DbType.String)
                {
                    Value = (object) blockHeader.PreviousId ?? DBNull.Value
                });
                command.Parameters.Add(new NpgsqlParameter("@minedAt", DbType.DateTime) {Value = blockHeader.MinedAt});

                await command.ExecuteNonQueryAsync();

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
            await using var context = _contextFactory.Invoke();

            var entity = context.BlockHeaders.SingleOrDefault(x => x.BlockchainId == blockchainId && x.Number == blockNumber);

            return entity != null ? MapFromEntity(entity) : null;
        }
        
        public async Task Remove(string globalId)
        {
            await using var context = _contextFactory.Invoke();

            await context.BlockHeaders.Where(x => x.GlobalId == globalId).DeleteAsync();
        }

        public async Task<IEnumerable<BlockHeader>> GetBatch(string blockchainId, long startBlockNumber, int limit)
        {
            await using var context = _contextFactory.Invoke();

            var entities = await context.BlockHeaders
                .Where(x => x.BlockchainId == blockchainId && x.Number >= startBlockNumber)
                .OrderBy(x => x.Number)
                .Take(limit)
                .ToArrayAsync();

            return entities.Select(MapFromEntity);
        }

        private static BlockHeader MapFromEntity(BlockHeaderEntity entity)
        {
            return new BlockHeader(
                entity.BlockchainId,
                entity.Id,
                entity.Number,
                entity.PreviousId,
                new DateTime(entity.MinedAt.Ticks, DateTimeKind.Utc));
        }
    }
}
