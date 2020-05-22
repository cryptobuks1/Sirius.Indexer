using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Indexer.Common.Persistence
{
    public class BlockchainsRepository : IBlockchainsRepository
    {
        private readonly DbContextOptionsBuilder<DatabaseContext> _dbContextOptionsBuilder;

        public BlockchainsRepository(DbContextOptionsBuilder<DatabaseContext> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task<IReadOnlyCollection<BlockchainMetamodel>> GetAllAsync(string cursor, int limit)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var query = context.Blockchains.Select(x => x);

            if (cursor != null)
            {
                // ReSharper disable once StringCompareToIsCultureSpecific
                query = query.Where(x => x.Id.CompareTo(cursor) > 1);
            }

            return await query
                .OrderBy(x => x.Id)
                .Take(limit)
                .ToListAsync();
        }

        public async Task AddOrReplaceAsync(BlockchainMetamodel blockchainMetamodel)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            try
            {
                context.Blockchains.Add(blockchainMetamodel);

                await context.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                context.Blockchains.Update(blockchainMetamodel);

                await context.SaveChangesAsync();
            }
        }

        public async Task<BlockchainMetamodel> GetAsync(string blockchainId)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var result = await context.Blockchains.FirstAsync(x => x.Id == blockchainId);
            return result;
        }
    }
}
