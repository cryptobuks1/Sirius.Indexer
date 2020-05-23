using System;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Messaging.InMemoryBus;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class FirstPassHistoryIndexer
    {
        private FirstPassHistoryIndexer(FirstPassHistoryIndexerId id,
            long stopBlock,
            long nextBlock,
            int version)
        {
            Id = id;
            StopBlock = stopBlock;
            NextBlock = nextBlock;
            Version = version;
        }

        public FirstPassHistoryIndexerId Id { get; }
        public string BlockchainId => Id.BlockchainId;
        public long StartBlock => Id.StartBlock;
        public long StopBlock { get; }
        public long NextBlock { get; private set; }
        public int Version { get; }
        public bool IsCompleted => NextBlock >= StopBlock;
        
        public static FirstPassHistoryIndexer Start(FirstPassHistoryIndexerId id, long stopBlock)
        {
            return new FirstPassHistoryIndexer(
                id,
                stopBlock: stopBlock,
                nextBlock: id.StartBlock,
                version: 0);
        }

        public async Task<FirstPassHistoryIndexingResult> IndexNextBlock(ILogger<FirstPassHistoryIndexer> logger,
            IBlocksReader blocksReader,
            IBlocksRepository blocksRepository,
            IInMemoryBus inMemoryBus)
        {
            if (IsCompleted)
            {
                return FirstPassHistoryIndexingResult.IndexingCompleted;
            }

            var block = await blocksReader.ReadBlockOrDefaultAsync(NextBlock);

            if (block == null)
            {
                logger.LogInformation($"First-pass history indexer has not found the block. Likely `{nameof(BlockchainIndexingConfig.LastHistoricalBlockNumber)}` should be decreased. It should be existing block {{@context}}", new
                {
                    BlockchainId = BlockchainId,
                    BlockNumber = NextBlock
                });

                throw new InvalidOperationException($@"First-pass history indexer {BlockchainId} has not found the block {NextBlock}.");
            }

            await blocksRepository.InsertOrIgnore(block);

            // TODO: Add first-pass block data indexing
            
            await inMemoryBus.Publish(new FirstPassHistoryBlockDetected
            {
                BlockchainId = BlockchainId
            });
            
            NextBlock++;

            return FirstPassHistoryIndexingResult.BlockIndexed;
        }
    }
}
