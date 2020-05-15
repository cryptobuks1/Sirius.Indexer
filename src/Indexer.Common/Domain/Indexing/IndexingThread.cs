using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Domain.Indexing
{
    public class IndexingThread
    {
        private readonly List<object> _events = new List<object>();

        private IndexingThread(string blockchainId, 
            long startBlock, 
            long stopBlock,
            long nextBlock,
            long sequence,
            int version)
        {
            Id = $"{blockchainId}-{startBlock}";

            BlockchainId = blockchainId;
            StartBlock = startBlock;
            StopBlock = stopBlock;
            NextBlock = nextBlock;
            Sequence = sequence;
            Version = version;
        }

        public string Id { get; }
        public string BlockchainId { get; }
        public long StartBlock { get; }
        public long StopBlock { get; }
        public long NextBlock { get; private set; }
        public long Sequence { get; private set; }
        public int Version { get; }
        public bool IsReversible => StopBlock == long.MaxValue;
        public IReadOnlyCollection<object> Events => _events;

        public static IndexingThread StartIrreversible(string blockchainId, long startBlock, long stopBlock)
        {
            return new IndexingThread(
                blockchainId,
                startBlock,
                stopBlock,
                startBlock,
                sequence: 0,
                version: 0);
        }

        public static IndexingThread StartReversible(string blockchainId, long startBlock)
        {
            return new IndexingThread(
                blockchainId,
                startBlock,
                stopBlock: long.MaxValue,
                startBlock,
                sequence: 0,
                version: 0);
        }

        public async Task<BlockIndexingResult> IndexNextBlock(ILogger<IndexingThread> logger,
            IBlocksReader reader,
            BlockProcessor processor)
        {
            var block = await reader.ReadBlockOrDefaultAsync(NextBlock);

            if (block == null)
            {
                return BlockIndexingResult.BlockNotFound;
            }

            var processingResult = await processor.ProcessBlock(StartBlock, block);

            switch (processingResult.IndexingDirection)
            {
                case IndexingDirection.Forward:
                    //await block.ExecuteAsync(this.Blockchain, this.NetworkType, this.executionRouter);
                    //await this.chainHeightChangingPublisher.Publish(new ChainHeightIncreased
                    //{
                    //    Blockchain = BlockchainMapping.FromConstant(this.Blockchain),
                    //    Network = NetworkMapping.FromDomain(this.NetworkType),
                    //    ChainSequence = blockNumber.Sequence,
                    //    BlockHash = block.Header.Hash,
                    //    BlockNumber = block.Header.Number,
                    //    PreviousBlockHash = block.Header.PreviousHash
                    //});
                    NextBlock++;
                    Sequence++;
                    break;

                case IndexingDirection.Backward:
                    //await this.canceler.Cancel(processingResult.PreviousBlockHash);
                    
                    //await this.chainHeightChangingPublisher.Publish(new ChainHeightDecreased
                    //{
                    //    Blockchain = BlockchainMapping.FromConstant(this.Blockchain),
                    //    Network = NetworkMapping.FromDomain(this.NetworkType),
                    //    ChainSequence = blockNumber.Sequence,
                    //    RevertedBlockHash = processingResult.PreviousBlockHash,
                    //});
                    NextBlock--;
                    Sequence++;

                    logger.LogInformation("Block has been reverted {@context}", new
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
    }
}
