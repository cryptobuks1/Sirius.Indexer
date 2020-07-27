using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    internal class NonceFirstPassIndexingStrategy : IFirstPasseIndexingStrategy
    {
        private readonly ILogger<NonceFirstPassIndexingStrategy> _logger;
        private readonly IBlocksReader _blocksReader;
        private readonly NonceBlockAssetsProvider _blockAssetsProvider;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;

        public NonceFirstPassIndexingStrategy(ILogger<NonceFirstPassIndexingStrategy> logger,
            IBlocksReader blocksReader,
            NonceBlockAssetsProvider blockAssetsProvider,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory)
        {
            _logger = logger;
            _blocksReader = blocksReader;
            _blockAssetsProvider = blockAssetsProvider;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
        }

        public async Task IndexNextBlock(FirstPassIndexer indexer)
        {
            var block = await _blocksReader.ReadNonceBlockOrDefault(indexer.NextBlock);

            if (block == null)
            {
                _logger.LogWarning($"First-pass indexer has not found the block. Likely `{nameof(BlockchainIndexingConfig.LastHistoricalBlockNumber)}` should be decreased. It should be existing block {{@context}}", new
                {
                    BlockchainId = indexer.BlockchainId,
                    StartBlock = indexer.StartBlock,
                    NextBlock = indexer.NextBlock
                });

                throw new InvalidOperationException($"First-pass indexer {indexer.Id} has not found the block {indexer.NextBlock}.");
            }

            await using var unitOfWork = await _blockchainDbUnitOfWorkFactory.Start(indexer.BlockchainId);

            var nonceUpdates = block.Transfers
                .SelectMany(tx => tx.NonceUpdates)
                .GroupBy(x => new
                {
                    x.Address,
                    x.BlockId
                })
                .Select(g => new NonceUpdate(
                    g.Key.Address,
                    g.Key.BlockId,
                    g.Max(x => x.Nonce)))
                .ToArray();

            // TODO: Save operations

            await unitOfWork.NonceUpdates.InsertOrIgnore(nonceUpdates);

            var operations = block.Transfers.SelectMany(tx => tx.Operations).ToArray();
            var blockSources = operations.SelectMany(x => x.Sources).ToArray();
            var blockDestinations = operations.SelectMany(x => x.Destinations).ToArray();
            var blockFeeSources = block.Transfers.SelectMany(x => x.Fees).ToArray();
            
            var blockAssets = await _blockAssetsProvider.Get(
                block.Header.BlockchainId,
                blockSources,
                blockDestinations,
                blockFeeSources);
            
            var fees = NonceFeesFactory.Create(block.Transfers, blockAssets);

            await unitOfWork.Fees.InsertOrIgnore(fees);

            var balanceUpdates = NonceBalanceUpdatesCalculator.Calculate(
                block.Header,
                blockSources,
                blockDestinations,
                blockFeeSources,
                blockAssets);

            await unitOfWork.BalanceUpdates.InsertOrIgnore(balanceUpdates);
            await unitOfWork.TransactionHeaders.InsertOrIgnore(block.Transfers.Select(x => x.Header).ToArray());
            await unitOfWork.BlockHeaders.InsertOrIgnore(block.Header);
        }
    }
}
