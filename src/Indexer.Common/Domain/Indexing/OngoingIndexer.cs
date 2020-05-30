using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.MessagingContract;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class OngoingIndexer
    {
        private OngoingIndexer(string blockchainId, 
            long startBlock, 
            long nextBlock, 
            long sequence, 
            DateTime startedAt,
            DateTime updatedAt,
            int version)
        {
            BlockchainId = blockchainId;
            StartBlock = startBlock;
            NextBlock = nextBlock;
            Sequence = sequence;
            StartedAt = startedAt;
            UpdatedAt = updatedAt;
            Version = version;
        }

        public string BlockchainId { get; }
        public long StartBlock { get; }
        public long NextBlock { get; private set; }
        public long Sequence { get; private set; }
        public DateTime StartedAt { get; }
        public DateTime UpdatedAt { get; private set; }
        public int Version { get; }
        
        public static OngoingIndexer Start(string blockchainId, long startBlock, long startSequence)
        {
            var now = DateTime.UtcNow;

            return new OngoingIndexer(
                blockchainId,
                startBlock,
                startBlock,
                startSequence,
                now,
                now,
                version: 0);
        }

        public static OngoingIndexer Restore(string blockchainId,
            long startBlock,
            long nextBlock,
            long sequence,
            DateTime startedAt,
            DateTime updatedAt,
            int version)
        {
            return new OngoingIndexer(
                blockchainId,
                startBlock,
                nextBlock,
                sequence,
                startedAt,
                updatedAt,
                version);
        }

        public async Task<IOngoingIndexingResult> IndexNextBlock(ILogger<OngoingIndexer> logger,
            IBlocksReader reader,
            BlocksProcessor processor,
            IPublishEndpoint publisher)
        {
            var newBlock = await reader.ReadBlockOrDefaultAsync(NextBlock);

            if (newBlock == null)
            {
                return OngoingIndexingResult.BlockNotFound();
            }

            var indexingResult = OngoingIndexingResult.BlockIndexed();
            var processingResult = await processor.ProcessBlock(newBlock);

            UpdatedAt = DateTime.UtcNow;

            switch (processingResult.IndexingDirection)
            {
                case IndexingDirection.Forward:
                    // This is needed to mitigate events publishing latency
                    indexingResult.AddBackgroundTask(
                        publisher.Publish(new BlockDetected
                        {
                            BlockchainId = BlockchainId,
                            BlockId = newBlock.Id,
                            BlockNumber = newBlock.Number,
                            PreviousBlockId = newBlock.PreviousId,
                            ChainSequence = Sequence
                        }));

                    // TODO:
                    //await block.ExecuteAsync(this.Blockchain, this.NetworkType, this.executionRouter);

                    NextBlock++;
                    Sequence++;
                    
                    logger.LogInformation("Ongoing indexer has indexed the block {@context}", new
                    {
                        BlockchainId = BlockchainId,
                        BlockNumber = newBlock.Number,
                        BlockId = newBlock.Id
                    });

                    break;

                case IndexingDirection.Backward:
                    // This is needed to mitigate events publishing latency
                    indexingResult.AddBackgroundTask(
                        publisher.Publish(new BlockCancelled
                        {
                            BlockchainId = BlockchainId,
                            BlockId = processingResult.PreviousBlockHeader.Id,
                            BlockNumber = processingResult.PreviousBlockHeader.Number,
                            ChainSequence = Sequence
                        }));

                    // TODO:
                    //await this.canceler.Cancel(processingResult.PreviousBlockHash);

                    NextBlock--;
                    Sequence++;

                    logger.LogInformation("Ongoing indexer has reverted the block {@context}", new
                    {
                        BlockchainId = BlockchainId,
                        BlockNumber = processingResult.PreviousBlockHeader.Number,
                        BlockId = processingResult.PreviousBlockHeader.Id
                    });

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(processingResult.IndexingDirection), processingResult.IndexingDirection, string.Empty);
            }

            return indexingResult;
        }
    }
}
