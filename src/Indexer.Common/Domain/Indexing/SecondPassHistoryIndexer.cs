using System.Threading.Tasks;
using MassTransit;
using Swisschain.Sirius.Indexer.MessagingContract;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class SecondPassHistoryIndexer
    {
        private SecondPassHistoryIndexer(string blockchainId,
            long nextBlock,
            long stopBlock,
            long sequence,
            long version)
        {
            BlockchainId = blockchainId;
            NextBlock = nextBlock;
            StopBlock = stopBlock;
            Sequence = sequence;
            Version = version;
        }

        public string BlockchainId { get; }
        public long NextBlock { get; private set; }
        public long StopBlock { get; }
        public long Sequence { get; private set; }
        public long Version { get; }
        public bool IsCompleted => NextBlock == StopBlock;

        public static SecondPassHistoryIndexer Create(string blockchainId, long startBlock, long stopBlock)
        {
            return new SecondPassHistoryIndexer(
                blockchainId,
                startBlock,
                stopBlock,
                sequence: 0,
                version: 0);
        }

        public async Task<SecondPassHistoryIndexingResult> IndexAvailableBlocks(int maxBlocksCount,
            IBlocksRepository blocksRepository,
            IPublishEndpoint publisher)
        {
            var blocks = await blocksRepository.GetBatch(BlockchainId, NextBlock, maxBlocksCount);

            foreach (var block in blocks)
            {
                if (IsCompleted)
                {
                    return SecondPassHistoryIndexingResult.IndexingCompleted;
                }

                if (NextBlock != block.Number)
                {
                    return SecondPassHistoryIndexingResult.IndexingInProgress;
                }

                await StepForward(block, publisher);
            }

            return SecondPassHistoryIndexingResult.IndexingInProgress;
        }
        
        private async Task StepForward(Block block, IPublishEndpoint publisher)
        {
            NextBlock = block.Number + 1;
            Sequence++;

            // TODO: Index block data

            await publisher.Publish(new BlockDetected
            {
                BlockchainId = BlockchainId,
                BlockId = block.Id,
                BlockNumber = block.Number,
                ChainSequence = Sequence,
                PreviousBlockId = block.PreviousId
            });
        }
    }
}
