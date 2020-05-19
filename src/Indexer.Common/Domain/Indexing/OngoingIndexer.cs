using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class OngoingIndexer
    {
        private OngoingIndexer(string blockchainId, long startBlock, long nextBlock, long sequence, int version)
        {
            BlockchainId = blockchainId;
            StartBlock = startBlock;
            NextBlock = nextBlock;
            Sequence = sequence;
            Version = version;
        }

        public string BlockchainId { get; }
        public long StartBlock { get; }
        public long NextBlock { get; private set; }
        public long Sequence { get; private set; }
        public int Version { get; }

        public static OngoingIndexer Start(string blockchainId, long startBlock, long sequence)
        {
            return new OngoingIndexer(
                blockchainId,
                startBlock,
                startBlock,
                sequence,
                version: 0);
        }

        public async Task<OngoingBlockIndexingResult> IndexNextBlock(ILogger<FirstPassHistoryIndexer> logger,
            IBlocksReader reader,
            BlocksProcessor processor)
        {
            var newBlock = await reader.ReadBlockOrDefaultAsync(NextBlock);

            if (newBlock == null)
            {
                return OngoingBlockIndexingResult.BlockNotFound;
            }

            var processingResult = await processor.ProcessBlock(StartBlock, newBlock);

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

                    logger.LogInformation("Block has been reverted by the ongoing indexer {@context}", new
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
