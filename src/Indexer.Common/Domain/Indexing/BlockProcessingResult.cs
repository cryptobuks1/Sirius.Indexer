using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class BlockProcessingResult
    {
        private BlockProcessingResult(IndexingDirection indexingDirection, BlockHeader previousBlockHeader)
        {
            IndexingDirection = indexingDirection;
            PreviousBlockHeader = previousBlockHeader;
        }

        public IndexingDirection IndexingDirection { get; }

        public BlockHeader PreviousBlockHeader { get; }

        public static BlockProcessingResult CreateForward()
        {
            return new BlockProcessingResult(IndexingDirection.Forward, null);
        }

        public static BlockProcessingResult CreateBackward(BlockHeader previousBlockHeader)
        {
            return new BlockProcessingResult(IndexingDirection.Backward, previousBlockHeader);
        }

        public override string ToString()
        {
            return IndexingDirection == IndexingDirection.Forward
                ? $"{IndexingDirection}"
                : $"{IndexingDirection}:{PreviousBlockHeader}";
        }
    }
}
