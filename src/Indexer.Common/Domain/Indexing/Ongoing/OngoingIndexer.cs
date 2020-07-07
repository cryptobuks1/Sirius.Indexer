using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling;
using Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing;

namespace Indexer.Common.Domain.Indexing.Ongoing
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
        public DateTime StartedAt { get; private set; }
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

        public async Task<OngoingBlockIndexingResult> IndexNextBlock(ChainWalker chainWalker,
            OngoingIndexingStrategyFactory indexingStrategyFactory,
            BlockCancelerFactory blockCancelerFactory)
        {
            var indexingStrategy = await indexingStrategyFactory.Create(BlockchainId);
            var blockIndexingStrategy = await indexingStrategy.StartBlockIndexing(NextBlock);
            
            if (!blockIndexingStrategy.IsBlockFound)
            {
                return OngoingBlockIndexingResult.BlockNotFound;
            }

            ChainWalkerMovement chainWalkerMovement;
            
            if (NextBlock == StartBlock)
            {
                StartedAt = UpdatedAt;
                chainWalkerMovement = ChainWalkerMovement.CreateForward();
            }
            else
            {
                chainWalkerMovement = await chainWalker.MoveTo(blockIndexingStrategy.BlockHeader);
            }

            UpdatedAt = DateTime.UtcNow;

            switch (chainWalkerMovement.Direction)
            {
                case MovementDirection.Forward:
                    await MoveForward(blockIndexingStrategy);

                    break;

                case MovementDirection.Backward:
                    await MoveBackward(blockCancelerFactory, chainWalkerMovement.EvictedBlockHeader);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(chainWalkerMovement.Direction), chainWalkerMovement.Direction, string.Empty);
            }

            return OngoingBlockIndexingResult.BlockIndexed;
        }

        private async Task MoveForward(IOngoingBlockIndexingStrategy blockIndexingStrategy)
        {
            await blockIndexingStrategy.ApplyBlock(this);

            NextBlock++;
            Sequence++;
        }

        private async Task MoveBackward(BlockCancelerFactory blockCancelerFactory, BlockHeader evictedBlockHeader)
        {
            var blockCanceler = await blockCancelerFactory.Create(BlockchainId);

            await blockCanceler.Cancel(this, evictedBlockHeader);
            
            NextBlock--;
            Sequence++;
        }
    }
}
