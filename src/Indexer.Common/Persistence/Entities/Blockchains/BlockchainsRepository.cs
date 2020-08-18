using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Persistence.EntityFramework;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Swisschain.Sirius.Sdk.Integrations.Contract.Integration;
using Z.EntityFramework.Plus;

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

        public async Task Upsert(BlockchainMetamodel blockchainMetamodel)
        {
            int affectedRowsCount = 0;
            await using var context = _contextFactory.Invoke();

            if (blockchainMetamodel.CreatedAt != blockchainMetamodel.UpdatedAt)
            {
                affectedRowsCount = await context.Blockchains
                    .Where(x => x.Id == blockchainMetamodel.Id &&
                                x.UpdatedAt <= blockchainMetamodel.UpdatedAt)
                    .UpdateAsync(x => new BlockchainMetamodel
                    {
                       Protocol = blockchainMetamodel.Protocol,
                       NetworkType = blockchainMetamodel.NetworkType,
                       CreatedAt = blockchainMetamodel.CreatedAt,
                       Id = blockchainMetamodel.Id,
                       IntegrationUrl = blockchainMetamodel.IntegrationUrl,
                       Name = blockchainMetamodel.Name,
                       TenantId = blockchainMetamodel.TenantId,
                       UpdatedAt = blockchainMetamodel.UpdatedAt
                    });
            }

            if (affectedRowsCount == 0)
            {
                try
                {
                    context.Blockchains.Add(blockchainMetamodel);
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateException e) when (e.InnerException is PostgresException pgEx
                                                  && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    //Swallow error: the entity was already added
                }
            }
        }

        public async Task<BlockchainMetamodel> GetAsync(string blockchainId)
        {
            await using var context = _contextFactory.Invoke();

            var result = await context.Blockchains.SingleAsync(x => x.Id == blockchainId);
            return result;
        }

        public async Task<BlockchainMetamodel> GetOrDefaultAsync(string blockchainId)
        {
            await using var context = _contextFactory.Invoke();

            var result = await context.Blockchains.SingleOrDefaultAsync(x => x.Id == blockchainId);
            return result;
        }
    }
}
