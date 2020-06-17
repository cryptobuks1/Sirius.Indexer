using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing.Ongoing
{
    public sealed class ChainWalkerMovement
    {
        private ChainWalkerMovement(MovementDirection direction, BlockHeader evictedBlockHeader)
        {
            Direction = direction;
            EvictedBlockHeader = evictedBlockHeader;
        }

        public MovementDirection Direction { get; }

        public BlockHeader EvictedBlockHeader { get; }

        public static ChainWalkerMovement CreateForward()
        {
            return new ChainWalkerMovement(MovementDirection.Forward, null);
        }

        public static ChainWalkerMovement CreateBackward(BlockHeader evictedBlockHeader)
        {
            return new ChainWalkerMovement(MovementDirection.Backward, evictedBlockHeader);
        }

        public override string ToString()
        {
            return Direction == MovementDirection.Forward
                ? $"{Direction}"
                : $"{Direction}:{EvictedBlockHeader}";
        }
    }
}
