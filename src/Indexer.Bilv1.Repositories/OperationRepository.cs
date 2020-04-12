using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Bilv1.Domain.Models.EnrolledBalances;
using Indexer.Bilv1.Domain.Models.Operations;
using Indexer.Bilv1.Domain.Repositories;
using Indexer.Bilv1.Repositories.DbContexts;
using Indexer.Bilv1.Repositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace Indexer.Bilv1.Repositories
{
    public class OperationRepository : IOperationRepository
    {
        private readonly DbContextOptionsBuilder<IndexerBilV1Context> _dbContextOptionsBuilder;

        public OperationRepository(DbContextOptionsBuilder<IndexerBilV1Context> dbContextOptionsBuilder)
        {
            _dbContextOptionsBuilder = dbContextOptionsBuilder;
        }

        public async Task<Operation> AddAsync(DepositWalletKey key, decimal balanceChange, long block)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var entity = new OperationEntity()
                {
                    BlockchainAssetId = key.BlockchainAssetId,
                    BlockchianId = key.BlockchainId,
                    BalanceChange = balanceChange,
                    BlockNumber = block,
                    WalletAddress = key.WalletAddress.ToLower(CultureInfo.InvariantCulture),
                    OriginalWalletAddress = key.WalletAddress
                };
                context.Operations.Add(entity);

                await context.SaveChangesAsync();

                return MapFromOperationEntity(entity);
            }
        }

        public async Task<IEnumerable<Operation>> GetAllForBlockchainAsync(string blockchainId, int skip, int take)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var result = context.Operations.Where(x =>
                        x.BlockchianId == blockchainId )
                    .OrderBy(x => x.OperationId)
                    .Skip(skip)
                    .Take(take);

                await result.LoadAsync();

                return result.Select(MapFromOperationEntity).ToList();
            }
        }

        public async Task<IEnumerable<Operation>> GetAsync(DepositWalletKey key, int skip, int take)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var result = context.Operations.Where(x => 
                        x.BlockchainAssetId == key.BlockchainAssetId &&
                        x.BlockchianId == key.BlockchainId &&
                        x.WalletAddress == key.WalletAddress.ToLower(CultureInfo.InvariantCulture))
                    .OrderBy(x => x.OperationId)
                    .Skip(skip)
                    .Take(take);

                await result.LoadAsync();

                return result.Select(MapFromOperationEntity).ToList();
            }
        }

        public async Task<IEnumerable<Operation>> GetAsync(string blockchainId, string walletAddress, int skip, int take)
        {
            using (var context = new IndexerBilV1Context(_dbContextOptionsBuilder.Options))
            {
                var result = context.Operations.Where(x =>
                        x.BlockchianId == blockchainId &&
                        x.WalletAddress == walletAddress.ToLower(CultureInfo.InvariantCulture))
                    .OrderBy(x => x.OperationId)
                    .Skip(skip)
                    .Take(take);

                await result.LoadAsync();

                return result.Select(MapFromOperationEntity).ToList();
            }
        }

        private static Operation MapFromOperationEntity(OperationEntity operation)
        {
            return Operation.Create(
                new DepositWalletKey(operation.BlockchainAssetId, operation.BlockchianId, 
                    operation.OriginalWalletAddress),
                operation.BalanceChange,
                operation.BlockNumber,
                operation.OperationId);
        }
    }
}
