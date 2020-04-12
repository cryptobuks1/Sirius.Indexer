using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public async Task<IReadOnlyCollection<Blockchain>> GetAllAsync(string cursor, int limit)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var query = context.Blockchains.Select(x => x);

            if (cursor != null)
            {
                // ReSharper disable once StringCompareToIsCultureSpecific
                query = query.Where(x => x.BlockchainId.CompareTo(cursor) > 1);
            }

            return await query
                .OrderBy(x => x.BlockchainId)
                .Take(limit)
                .ToListAsync();
        }

        public async Task AddOrReplaceAsync(Blockchain blockchain)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            try
            {
                context.Blockchains.Add(blockchain);

                await context.SaveChangesAsync();
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                context.Blockchains.Update(blockchain);

                await context.SaveChangesAsync();
            }
        }

        public async Task<Blockchain> GetAsync(string blockchainId)
        {
            await using var context = new DatabaseContext(_dbContextOptionsBuilder.Options);

            var result = await context.Blockchains.FirstAsync(x => x.BlockchainId == blockchainId);
            return result;
        }
    }
}
