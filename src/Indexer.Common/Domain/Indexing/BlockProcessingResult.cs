namespace Indexer.Common.Domain.Indexing
{
    public sealed class BlockProcessingResult
    {
        private BlockProcessingResult(IndexingDirection indexingDirection, Block previousBlock)
        {
            IndexingDirection = indexingDirection;
            PreviousBlock = previousBlock;
        }

        public IndexingDirection IndexingDirection { get; }

        public Block PreviousBlock { get; }

        public static BlockProcessingResult CreateForward()
        {
            return new BlockProcessingResult(IndexingDirection.Forward, null);
        }

        public static BlockProcessingResult CreateBackward(Block previousBlock)
        {
            return new BlockProcessingResult(IndexingDirection.Backward, previousBlock);
        }

        public override string ToString()
        {
            return IndexingDirection == IndexingDirection.Forward
                ? $"{IndexingDirection}"
                : $"{IndexingDirection}:{PreviousBlock}";
        }
    }
}
