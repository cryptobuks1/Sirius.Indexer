using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions.Transfers.Coins;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    internal class CoinsFirstPassIndexingStrategy : IFirstPasseIndexingStrategy
    {
        private readonly ILogger<CoinsFirstPassIndexingStrategy> _logger;
        private readonly IBlocksReader _blocksReader;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly UnspentCoinsFactory _unspentCoinsFactory;

        public CoinsFirstPassIndexingStrategy(ILogger<CoinsFirstPassIndexingStrategy> logger,
            IBlocksReader blocksReader,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            UnspentCoinsFactory unspentCoinsFactory)
        {
            _logger = logger;
            _blocksReader = blocksReader;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _unspentCoinsFactory = unspentCoinsFactory;
        }

        public async Task IndexNextBlock(FirstPassIndexer indexer)
        {
            var block = await _blocksReader.ReadCoinsBlockOrDefault(indexer.NextBlock);

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
            
            await unitOfWork.InputCoins.InsertOrIgnore(block.Transfers.SelectMany(x => x.InputCoins).ToArray());

            var outputCoins = await _unspentCoinsFactory.Create(block.Transfers);

            await unitOfWork.UnspentCoins.InsertOrIgnore(outputCoins);
            await unitOfWork.TransactionHeaders.InsertOrIgnore(block.Transfers.Select(x => x.Header).ToArray());
            
            // Header should be the last persisted part of the block, since the second-pass processor check headers,
            // to decide if a new block is ready to process.
            await unitOfWork.BlockHeaders.InsertOrIgnore(block.Header);
        }
    }
}