using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Messaging.InMemoryBus;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    public sealed class FirstPassIndexer
    {
        private FirstPassIndexer(FirstPassIndexerId id,
            long stopBlock,
            long nextBlock,
            long stepSize,
            DateTime startedAt,
            DateTime updatedAt,
            int version)
        {
            Id = id;
            StopBlock = stopBlock;
            NextBlock = nextBlock;
            StepSize = stepSize;
            StartedAt = startedAt;
            UpdatedAt = updatedAt;
            Version = version;
        }

        public FirstPassIndexerId Id { get; }
        public string BlockchainId => Id.BlockchainId;
        public long StartBlock => Id.StartBlock;
        public long StopBlock { get; }
        public long NextBlock { get; private set; }
        public long StepSize { get; }
        public DateTime StartedAt { get; }
        public DateTime UpdatedAt { get; private set; }
        public int Version { get; }
        public bool IsCompleted => NextBlock >= StopBlock;
        
        public static FirstPassIndexer Start(FirstPassIndexerId id, long stopBlock, long stepSize)
        {
            var now = DateTime.UtcNow;

            return new FirstPassIndexer(
                id,
                stopBlock: stopBlock,
                nextBlock: id.StartBlock,
                stepSize: stepSize,
                now,
                now,
                version: 0);
        }

        public static FirstPassIndexer Restore(
            FirstPassIndexerId id,
            long stopBlock,
            long nextBlock,
            long stepSize,
            DateTime startedAt,
            DateTime updatedAt,
            int version)
        {
            return new FirstPassIndexer(
                id,
                stopBlock,
                nextBlock,
                stepSize,
                startedAt,
                updatedAt,
                version);
        }

        public async Task<FirstPassIndexingResult> IndexNextBlock(ILogger<FirstPassIndexer> logger,
            IBlocksReader blocksReader,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            UnspentCoinsFactory unspentCoinsFactory,
            IInMemoryBus inMemoryBus)
        {
            if (IsCompleted)
            {
                return FirstPassIndexingResult.IndexingCompleted;
            }

            var block = await blocksReader.ReadCoinsBlockOrDefault(NextBlock);

            if (block == null)
            {
                logger.LogWarning($"First-pass indexer has not found the block. Likely `{nameof(BlockchainIndexingConfig.LastHistoricalBlockNumber)}` should be decreased. It should be existing block {{@context}}", new
                {
                    BlockchainId = BlockchainId,
                    StartBlock = StartBlock,
                    NextBlock = NextBlock
                });

                throw new InvalidOperationException($"First-pass indexer {Id} has not found the block {NextBlock}.");
            }

            await using var unitOfWork = await blockchainDbUnitOfWorkFactory.Start(BlockchainId);
            
            await unitOfWork.InputCoins.InsertOrIgnore(block.Transfers.SelectMany(x => x.InputCoins).ToArray());

            var outputCoins = await unspentCoinsFactory.Create(block.Transfers);

            await unitOfWork.UnspentCoins.InsertOrIgnore(outputCoins);
            await unitOfWork.TransactionHeaders.InsertOrIgnore(block.Transfers.Select(x => x.Header).ToArray());
            
            // Header should be the last persisted part of the block, since the second-pass processor check headers,
            // to decide if a new block is ready to process.
            await unitOfWork.BlockHeaders.InsertOrIgnore(block.Header);

            NextBlock += StepSize;
            UpdatedAt = DateTime.UtcNow;

            await inMemoryBus.Publish(new FirstPassBlockDetected
            {
                BlockchainId = BlockchainId
            });
            
            return IsCompleted ? FirstPassIndexingResult.IndexingCompleted : FirstPassIndexingResult.BlockIndexed;
        }
    }
}
