using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain;
using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Z.EntityFramework.Plus;

namespace Indexer.Common.Persistence
{
    internal class BlocksRepository : IBlocksRepository
    {
        private readonly Func<DatabaseContext> _contextFactory;

        public BlocksRepository(Func<DatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task InsertOrIgnore(Block block)
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
insert into {DatabaseContext.SchemaName}.{TableNames.Blocks} 
(
    ""GlobalId"",
    ""BlockchainId"",
    ""Id"",
    ""Number"",
    ""PreviousId""
) 
values 
(
    @globalId,
    @blockchainId,
    @id,
    @number,
    @previousId
) 
on conflict (""GlobalId"") do nothing";

                command.Parameters.Add(new NpgsqlParameter("@globalId", DbType.String) {Value = block.GlobalId});
                command.Parameters.Add(
                    new NpgsqlParameter("@blockchainId", DbType.String) {Value = block.BlockchainId});
                command.Parameters.Add(new NpgsqlParameter("@id", DbType.String) {Value = block.Id});
                command.Parameters.Add(new NpgsqlParameter("@number", DbType.Int64) {Value = block.Number});
                command.Parameters.Add(
                    new NpgsqlParameter("@previousId", DbType.String)
                    {
                        Value = (object) block.PreviousId ?? DBNull.Value
                    });

                await command.ExecuteNonQueryAsync();

                telemetry.Complete();
            }
            catch (Exception ex)
            {
                telemetry.Fail(ex);
            }
        }

        public async Task<Block> GetOrDefault(string blockchainId, long blockNumber)
        {
            await using var context = _contextFactory.Invoke();

            var entity = context.Blocks.SingleOrDefault(x => x.BlockchainId == blockchainId && x.Number == blockNumber);

            return entity != null ? MapFromEntity(entity) : null;
        }
        
        public async Task Remove(string globalId)
        {
            await using var context = _contextFactory.Invoke();

            await context.Blocks.Where(x => x.GlobalId == globalId).DeleteAsync();
        }

        public async Task<IEnumerable<Block>> GetBatch(string blockchainId, long startBlockNumber, int limit)
        {
            await using var context = _contextFactory.Invoke();

            var entities = await context.Blocks
                .Where(x => x.BlockchainId == blockchainId && x.Number >= startBlockNumber)
                .OrderBy(x => x.Number)
                .Take(limit)
                .ToArrayAsync();

            return entities.Select(MapFromEntity);
        }

        private static Block MapFromEntity(BlockEntity entity)
        {
            return new Block(
                entity.BlockchainId,
                entity.Id,
                entity.Number,
                entity.PreviousId);
        }
    }
}
