using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Microsoft.Extensions.Logging;

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

        public async Task<OngoingBlockIndexingResult> IndexNextBlock(ILogger<OngoingIndexer> logger,
            IBlocksReader reader,
            ChainWalker chainWalker,
            CoinsBlockApplier coinsBlockApplier,
            CoinsBlockCanceler coinsBlockCanceler)
        {
            var newBlock = await reader.ReadCoinsBlockOrDefault(NextBlock);

            if (newBlock == null)
            {
                return OngoingBlockIndexingResult.BlockNotFound;
            }

            var chainWalkerMovement = await chainWalker.MoveTo(newBlock.Header);

            UpdatedAt = DateTime.UtcNow;

            switch (chainWalkerMovement.Direction)
            {
                case MovementDirection.Forward:
                    await MoveForward(coinsBlockApplier, newBlock);

                    break;

                case MovementDirection.Backward:
                    await MoveBackward(coinsBlockCanceler, chainWalkerMovement.EvictedBlockHeader);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(chainWalkerMovement.Direction), chainWalkerMovement.Direction, string.Empty);
            }

            return OngoingBlockIndexingResult.BlockIndexed;
        }

        private async Task MoveForward(CoinsBlockApplier coinsBlockApplier, CoinsBlock newBlock)
        {
            await coinsBlockApplier.Apply(this, newBlock);

            NextBlock++;
            Sequence++;
        }

        private async Task MoveBackward(CoinsBlockCanceler coinsBlockCanceler, BlockHeader evictedBlockHeader)
        {
            await coinsBlockCanceler.Cancel(this, evictedBlockHeader);
            
            NextBlock--;
            Sequence++;
        }
    }
}
