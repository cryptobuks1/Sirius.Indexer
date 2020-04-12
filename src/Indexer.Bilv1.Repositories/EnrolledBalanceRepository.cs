using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Bilv1.Domain.Models.EnrolledBalances;
using Indexer.Bilv1.Domain.Repositories;
using Indexer.Bilv1.Repositories.DbContexts;
using Indexer.Bilv1.Repositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace Indexer.Bilv1.Repositories
{
    public class EnrolledBalanceRepository : IEnrolledBalanceRepository
    {
        private readonly DbContextOptionsBuilder<IndexerBilV1Context> _dbContextOptionsBuilder;

        public EnrolledBalanceRepository(DbContextOptionsBuilder<IndexerBilV1Context> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task<IReadOnlyCollection<EnrolledBalance>> GetAsync(IEnumerable<DepositWalletKey> keys)
        {
            var list = new List<EnrolledBalance>();

            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                foreach (var key in keys)
                {
                    var result = await context
                        .EnrolledBalances
                        .FindAsync(key.BlockchainId, key.BlockchainAssetId,
                            key.WalletAddress.ToLower(CultureInfo.InvariantCulture));

                    if (result == null)
                        continue;

                    list.Add(MapFromEntity(result));
                }
            }

            return list;
        }

        public async Task SetBalanceAsync(DepositWalletKey key, decimal balance, long balanceBlock)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var existing = await context.EnrolledBalances.FindAsync(key.BlockchainId, key.BlockchainAssetId,
                    key.WalletAddress.ToLower(CultureInfo.InvariantCulture));

                if (existing != null)
                {
                    existing.Balance = balance;
                    existing.BlockNumber = balanceBlock;

                    context.Update(existing);
                }
                else
                {
                    var newEntity = new EnrolledBalanceEntity
                    {
                        BlockchianId = key.BlockchainId,
                        BlockchainAssetId = key.BlockchainAssetId,
                        WalletAddress = key.WalletAddress.ToLower(CultureInfo.InvariantCulture),
                        Balance = balance,
                        BlockNumber = balanceBlock,
                        OriginalWalletAddress = key.WalletAddress
                    };

                    context.EnrolledBalances.Add(newEntity);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task ResetBalanceAsync(DepositWalletKey key, long transactionBlock)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var existing = await context.EnrolledBalances.FindAsync(key.BlockchainId, key.BlockchainAssetId,
                    key.WalletAddress.ToLower(CultureInfo.InvariantCulture));

                if (existing != null)
                {
                    context.EnrolledBalances.Update(existing);
                }
                else
                {
                    var newEntity = new EnrolledBalanceEntity
                    {
                        BlockchianId = key.BlockchainId,
                        BlockchainAssetId = key.BlockchainAssetId,
                        WalletAddress = key.WalletAddress.ToLower(CultureInfo.InvariantCulture),
                        Balance = 0,
                        BlockNumber = transactionBlock,
                        OriginalWalletAddress = key.WalletAddress
                    };

                    context.EnrolledBalances.Add(newEntity);
                }

                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteBalanceAsync(DepositWalletKey key)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var result = await context.EnrolledBalances.FindAsync(
                    key.BlockchainId, key.BlockchainAssetId, key.WalletAddress.ToLower(CultureInfo.InvariantCulture));

                if (result != null)
                    context.EnrolledBalances.Remove(result);

                await context.SaveChangesAsync();
            }
        }

        public async Task<EnrolledBalance> TryGetAsync(DepositWalletKey key)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var result = await context.EnrolledBalances.FindAsync(
                    key.BlockchainId, key.BlockchainAssetId, key.WalletAddress.ToLower(CultureInfo.InvariantCulture));
                var mapped = MapFromEntity(result);

                return mapped;
            }
        }

        public async Task<IReadOnlyCollection<EnrolledBalance>> GetAllAsync(int skip, int count)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var result = context
                    .EnrolledBalances
                    .Skip(skip)
                    .Take(count);

                await result.LoadAsync();

                return result.Select(MapFromEntity).ToList();
            }
        }

        public async Task<IReadOnlyCollection<EnrolledBalance>> GetAllForBlockchainAsync(string blockchainId, int skip, int count)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var result = context
                    .EnrolledBalances
                    .Where(x => x.BlockchianId == blockchainId)
                    .Skip(skip)
                    .Take(count);

                await result.LoadAsync();

                return result.Select(MapFromEntity).ToList();
            }
        }

        private static EnrolledBalance MapFromEntity(EnrolledBalanceEntity entity)
        {
            if (entity == null)
                return null;

            return EnrolledBalance.Create(
                new DepositWalletKey(entity.BlockchainAssetId, entity.BlockchianId, entity.OriginalWalletAddress),
                entity.Balance,
                entity.BlockNumber);
        }
    }
}
