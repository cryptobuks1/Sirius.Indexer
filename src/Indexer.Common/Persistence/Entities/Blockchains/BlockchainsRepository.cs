using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Persistence.EntityFramework;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Indexer.Common.Persistence.Entities.Blockchains
{
    public class BlockchainsRepository : IBlockchainsRepository
    {
        private readonly Func<CommonDatabaseContext> _contextFactory;

        public BlockchainsRepository(Func<CommonDatabaseContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IReadOnlyCollection<BlockchainMetamodel>> GetAllAsync(string cursor, int limit)
        {
            await using var context = _contextFactory.Invoke();

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
            await using var context = _contextFactory.Invoke();

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
            await using var context = _contextFactory.Invoke();

            var result = await context.Blockchains.FirstAsync(x => x.Id == blockchainId);
            return result;
        }
    }
}
