using System;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Messaging.InMemoryBus;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
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
            IBlocksRepository blocksRepository,
            IInMemoryBus inMemoryBus)
        {
            if (IsCompleted)
            {
                return FirstPassIndexingResult.IndexingCompleted;
            }

            var block = await blocksReader.ReadBlockOrDefaultAsync(NextBlock);

            if (block == null)
            {
                logger.LogInformation($"First-pass indexer has not found the block. Likely `{nameof(BlockchainIndexingConfig.LastHistoricalBlockNumber)}` should be decreased. It should be existing block {{@context}}", new
                {
                    BlockchainId = BlockchainId,
                    StartBlock = StartBlock,
                    NextBlock = NextBlock
                });

                throw new InvalidOperationException($"First-pass indexer {Id} has not found the block {NextBlock}.");
            }

            await blocksRepository.InsertOrIgnore(block);

            // TODO: Add first-pass block data indexing
            
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
