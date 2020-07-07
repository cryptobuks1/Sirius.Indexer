using System;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    public sealed class FirstPassIndexer
    {
        private FirstPassIndexer(FirstPassIndexerId id,
            long stopBlock,
            long nextBlock,
            long stepSize,
            DateTime startedAt,
            DateTime updatedAt,
            int version)
        {
            Id = id;
            StopBlock = stopBlock;
            NextBlock = nextBlock;
            StepSize = stepSize;
            StartedAt = startedAt;
            UpdatedAt = updatedAt;
            Version = version;
        }

        public FirstPassIndexerId Id { get; }
        public string BlockchainId => Id.BlockchainId;
        public long StartBlock => Id.StartBlock;
        public long StopBlock { get; }
        public long NextBlock { get; private set; }
        public long StepSize { get; }
        public DateTime StartedAt { get; }
        public DateTime UpdatedAt { get; private set; }
        public int Version { get; }
        public bool IsCompleted => NextBlock >= StopBlock;
        
        public static FirstPassIndexer Start(FirstPassIndexerId id, long stopBlock, long stepSize)
        {
            var now = DateTime.UtcNow;

            return new FirstPassIndexer(
                id,
                stopBlock: stopBlock,
                nextBlock: id.StartBlock,
                stepSize: stepSize,
                now,
                now,
                version: 0);
        }

        public static FirstPassIndexer Restore(
            FirstPassIndexerId id,
            long stopBlock,
            long nextBlock,
            long stepSize,
            DateTime startedAt,
            DateTime updatedAt,
            int version)
        {
            return new FirstPassIndexer(
                id,
                stopBlock,
                nextBlock,
                stepSize,
                startedAt,
                updatedAt,
                version);
        }

        public async Task<FirstPassIndexingResult> IndexNextBlock(FirstPassIndexingStrategyFactory firstPassIndexingStrategyFactory)
        {
            if (IsCompleted)
            {
                return FirstPassIndexingResult.IndexingCompleted;
            }

            var indexingStrategy = await firstPassIndexingStrategyFactory.Create(BlockchainId);

            await indexingStrategy.IndexNextBlock(this);

            NextBlock += StepSize;
            UpdatedAt = DateTime.UtcNow;
            
            return IsCompleted ? FirstPassIndexingResult.IndexingCompleted : FirstPassIndexingResult.BlockIndexed;
        }
    }
}
