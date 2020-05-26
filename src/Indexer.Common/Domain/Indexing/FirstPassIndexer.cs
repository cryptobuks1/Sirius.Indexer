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
            int version)
        {
            Id = id;
            StopBlock = stopBlock;
            NextBlock = nextBlock;
            Version = version;
        }

        public FirstPassIndexerId Id { get; }
        public string BlockchainId => Id.BlockchainId;
        public long StartBlock => Id.StartBlock;
        public long StopBlock { get; }
        public long NextBlock { get; private set; }
        public int Version { get; }
        public bool IsCompleted => NextBlock >= StopBlock;
        
        public static FirstPassIndexer Start(FirstPassIndexerId id, long stopBlock)
        {
            return new FirstPassIndexer(
                id,
                stopBlock: stopBlock,
                nextBlock: id.StartBlock,
                version: 0);
        }

        public static FirstPassIndexer Restore(
            FirstPassIndexerId id,
            long stopBlock,
            long nextBlock,
            int version)
        {
            return new FirstPassIndexer(
                id,
                stopBlock,
                nextBlock,
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
                    BlockNumber = NextBlock
                });

                throw new InvalidOperationException($"First-pass indexer {BlockchainId} has not found the block {NextBlock}.");
            }

            await blocksRepository.InsertOrIgnore(block);

            // TODO: Add first-pass block data indexing
            
            await inMemoryBus.Publish(new FirstPassBlockDetected
            {
                BlockchainId = BlockchainId
            });
            
            NextBlock++;

            if (IsCompleted)
            {
                if (NextBlock > StopBlock)
                {
                    NextBlock = StopBlock;
                }

                return FirstPassIndexingResult.IndexingCompleted;
            }

            return FirstPassIndexingResult.BlockIndexed;
        }
    }
}
