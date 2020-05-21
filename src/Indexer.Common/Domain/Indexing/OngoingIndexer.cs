using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class OngoingIndexer
    {
        private OngoingIndexer(string blockchainId, long nextBlock, long sequence, int version)
        {
            BlockchainId = blockchainId;
            NextBlock = nextBlock;
            Sequence = sequence;
            Version = version;
        }

        public string BlockchainId { get; }
        public long NextBlock { get; private set; }
        public long Sequence { get; private set; }
        public int Version { get; }

        public static OngoingIndexer Create(string blockchainId, long startBlock, long startSequence)
        {
            return new OngoingIndexer(
                blockchainId,
                startBlock,
                startSequence,
                version: 0);
        }

        public async Task<OngoingBlockIndexingResult> IndexNextBlock(ILogger<OngoingIndexer> logger,
            IBlocksReader reader,
            BlocksProcessor processor)
        {
            var newBlock = await reader.ReadBlockOrDefaultAsync(NextBlock);

            if (newBlock == null)
            {
                return OngoingBlockIndexingResult.BlockNotFound;
            }

            var processingResult = await processor.ProcessBlock(newBlock);

            switch (processingResult.IndexingDirection)
            {
                case IndexingDirection.Forward:
                    // TODO:
                    //_events.Add(new FirstPassHistoryBlockDetected
                    //{
                    //    BlockchainId = BlockchainId,
                    //    BlockId = newBlock.Id,
                    //    BlockNumber = newBlock.Number,
                    //    PreviousBlockId = newBlock.PreviousId
                    //});
                    
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
                    // TODO:
                    //_events.Add(new FirstPassBlockCancelled
                    //{
                    //    BlockchainId = BlockchainId,
                    //    BlockId = processingResult.PreviousBlock.Id,
                    //    BlockNumber = processingResult.PreviousBlock.Number
                    //});

                    //await this.canceler.Cancel(processingResult.PreviousBlockHash);
                    
                    NextBlock--;
                    Sequence++;

                    logger.LogInformation("Ongoing indexer has reverted the block {@context}", new
                    {
                        BlockchainId = BlockchainId,
                        BlockNumber = processingResult.PreviousBlock.Number,
                        BlockId = processingResult.PreviousBlock.Id
                    });

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(processingResult.IndexingDirection), processingResult.IndexingDirection, string.Empty);
            }

            return OngoingBlockIndexingResult.BlockIndexed;
        }
    }
}
