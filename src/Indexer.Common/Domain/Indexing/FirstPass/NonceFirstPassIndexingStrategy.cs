using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers.Nonces;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    internal class NonceFirstPassIndexingStrategy : IFirstPasseIndexingStrategy
    {
        private readonly ILogger<NonceFirstPassIndexingStrategy> _logger;
        private readonly IBlocksReader _blocksReader;
        private readonly FeesFactory _feesFactory;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;

        public NonceFirstPassIndexingStrategy(ILogger<NonceFirstPassIndexingStrategy> logger,
            IBlocksReader blocksReader,
            FeesFactory feesFactory,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory)
        {
            _logger = logger;
            _blocksReader = blocksReader;
            _feesFactory = feesFactory;
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

            var nonceUpdates = block.Transfers.SelectMany(tx => tx.NonceUpdates).ToArray();

            await unitOfWork.NonceUpdates.InsertOrIgnore(nonceUpdates);

            var fees = await _feesFactory.Create(block.Transfers);

            await unitOfWork.Fees.InsertOrIgnore(fees);

            throw new NotImplementedException();
            //var balanceUpdates = await NonceBalanceUpdatesCalculator.Calculate(block);

            //await unitOfWork.TransactionHeaders.InsertOrIgnore(block.Transfers.Select(x => x.Header).ToArray());
            
            //// Header should be the last persisted part of the block, since the second-pass processor check headers,
            //// to decide if a new block is ready to process.
            //await unitOfWork.BlockHeaders.InsertOrIgnore(block.Header);

        }
    }
}
