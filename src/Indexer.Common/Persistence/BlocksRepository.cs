using System.Collections.Generic;
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
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;

        public BlocksRepository(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task InsertOrReplace(Block block)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var entity = MapToEntity(block);

            await context.Blocks.AddAsync(entity);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                context.Blocks.Update(entity);

                await context.SaveChangesAsync();
            }
        }

        public async Task<Block> GetOrDefault(string blockchainId, long blockNumber)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var entity = context.Blocks.SingleOrDefault(x => x.BlockchainId == blockchainId && x.Number == blockNumber);

            return entity != null ? MapFromEntity(entity) : null;
        }
        
        public async Task Remove(string globalId)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            await context.Blocks.Where(x => x.GlobalId == globalId).DeleteAsync();
        }

        public async Task<IEnumerable<Block>> GetBatch(string blockchainId, long startBlockNumber, int limit)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var entities = await context.Blocks
                .Where(x => x.BlockchainId == blockchainId && x.Number >= startBlockNumber)
                .OrderBy(x => x.Number)
                .Take(limit)
                .ToArrayAsync();

            return entities.Select(MapFromEntity);
        }

        private static BlockEntity MapToEntity(Block block)
        {
            return new BlockEntity
            {
                GlobalId = block.GlobalId,
                BlockchainId = block.BlockchainId,
                Id = block.Id,
                Number = block.Number,
                PreviousId = block.PreviousId
            };
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
