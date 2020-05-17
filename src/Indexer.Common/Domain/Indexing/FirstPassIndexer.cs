using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class FirstPassIndexer
    {
        private readonly List<object> _events = new List<object>();

        private FirstPassIndexer(FirstPassIndexerId id,
            long stopBlock,
            long nextBlock,
            long sequence,
            int version)
        {
            Id = id;
            StopBlock = stopBlock;
            NextBlock = nextBlock;
            Sequence = sequence;
            Version = version;
        }

        public FirstPassIndexerId Id { get; }
        public string BlockchainId => Id.BlockchainId;
        public long StartBlock => Id.StartBlock;
        public long StopBlock { get; }
        public long NextBlock { get; private set; }
        public long Sequence { get; private set; }
        public int Version { get; }
        public bool IsReversible => StopBlock == long.MaxValue;
        public bool IsCompleted => NextBlock >= StopBlock;
        public IReadOnlyCollection<object> Events => _events;
        
        public static FirstPassIndexer StartIrreversible(FirstPassIndexerId id, long stopBlock)
        {
            return new FirstPassIndexer(
                id,
                stopBlock: stopBlock,
                nextBlock: id.StartBlock,
                sequence: 0,
                version: 0);
        }

        public static FirstPassIndexer StartReversible(FirstPassIndexerId id)
        {
            return new FirstPassIndexer(
                id,
                stopBlock: long.MaxValue,
                nextBlock: id.StartBlock,
                sequence: 0,
                version: 0);
        }

        public async Task<BlockIndexingResult> IndexNextBlock(ILogger<FirstPassIndexer> logger,
            IBlocksReader reader,
            BlocksProcessor processor)
        {
            if (IsCompleted)
            {
                return BlockIndexingResult.ThreadCompleted;
            }

            var newBlock = await reader.ReadBlockOrDefaultAsync(NextBlock);

            if (newBlock == null)
            {
                return BlockIndexingResult.BlockNotFound;
            }

            var processingResult = await processor.ProcessBlock(StartBlock, newBlock);

            switch (processingResult.IndexingDirection)
            {
                case IndexingDirection.Forward:
                    _events.Add(new FirstPassBlockDetected
                    {
                        BlockchainId = BlockchainId,
                        BlockId = newBlock.Id,
                        BlockNumber = newBlock.Number,
                        PreviousBlockId = newBlock.PreviousId
                    });
                    
                    //await block.ExecuteAsync(this.Blockchain, this.NetworkType, this.executionRouter);
                    
                    NextBlock++;
                    Sequence++;
                    break;

                case IndexingDirection.Backward:

                    if (!IsReversible)
                    {
                        logger.LogWarning("Only reversible first-pass indexing tread can step backward {@context}",
                            new
                            {
                                IndexingThread = this,
                                NewBlock = new
                                {
                                    Id = newBlock.Id,
                                    Number = newBlock.Number,
                                    PreviousId = newBlock.PreviousId
                                }
                            });

                        throw new InvalidOperationException("Only reversible indexing thread can step backward");
                    }

                    _events.Add(new FirstPassBlockCancelled
                    {
                        BlockchainId = BlockchainId,
                        BlockId = processingResult.PreviousBlock.Id,
                        BlockNumber = processingResult.PreviousBlock.Number
                    });

                    //await this.canceler.Cancel(processingResult.PreviousBlockHash);
                    
                    NextBlock--;
                    Sequence++;

                    logger.LogInformation("Block has been reverted by the first-pass indexing thread {@context}", new
                    {
                        BlockchainId = BlockchainId,
                        BlockNumber = processingResult.PreviousBlock.Number,
                        BlockId = processingResult.PreviousBlock.Id
                    });

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(processingResult.IndexingDirection), processingResult.IndexingDirection, string.Empty);
            }

            return BlockIndexingResult.BlockIndexed;
        }

        public void ClearEvents()
        {
            _events.Clear();
        }
    }
}
