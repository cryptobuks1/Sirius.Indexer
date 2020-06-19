using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Domain.Indexing.Common.CoinBlocks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.MessagingContract;

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

        public async Task<IOngoingIndexingResult> IndexNextBlock(ILogger<OngoingIndexer> logger,
            IBlocksReader reader,
            ChainWalker chainWalker,
            PrimaryBlockProcessor primaryBlockProcessor,
            CoinsPrimaryBlockProcessor coinsPrimaryBlockProcessor,
            CoinsSecondaryBlockProcessor coinsSecondaryBlockProcessor,
            CoinsBlockCanceler coinsBlockCanceler,
            IPublishEndpoint publisher)
        {
            var newBlock = await reader.ReadCoinsBlockOrDefault(NextBlock);

            if (newBlock == null)
            {
                return OngoingIndexingResult.BlockNotFound();
            }

            var indexingResult = OngoingIndexingResult.BlockIndexed();
            var chainWalkerMovement = await chainWalker.MoveTo(newBlock.Header);

            UpdatedAt = DateTime.UtcNow;

            switch (chainWalkerMovement.Direction)
            {
                case MovementDirection.Forward:
                    await primaryBlockProcessor.Process(newBlock.Header, newBlock.Transfers.Select(x => x.Header).ToArray());
                    await coinsPrimaryBlockProcessor.Process(newBlock);
                    // TODO: We can pass some data from the primary block processor directly to the
                    // secondary block processor directly skipping reading them from the DB
                    await coinsSecondaryBlockProcessor.Process(newBlock.Header);

                    // This is needed to mitigate events publishing latency
                    indexingResult.AddBackgroundTask(
                        publisher.Publish(new BlockDetected
                        {
                            BlockchainId = BlockchainId,
                            BlockId = newBlock.Header.Id,
                            BlockNumber = newBlock.Header.Number,
                            PreviousBlockId = newBlock.Header.PreviousId,
                            ChainSequence = Sequence
                        }));

                    foreach (var transfer in newBlock.Transfers)
                    {
                        // TODO:
                        //indexingResult.AddBackgroundTask(
                        //    publisher.Publish(new TransactionDetected
                        //    {
                        //        BlockchainId = BlockchainId,
                        //        BlockId = newBlock.Header.Id,
                        //        BlockNumber = newBlock.Header.Number,
                        //        TransactionId = transfer.Header.Id,
                        //        TransactionNumber = transfer.Header.Number,
                        //        Error = transfer.Header.Error
                        //        // TODO: Rest of data
                        //    }));
                    }

                    NextBlock++;
                    Sequence++;
                    
                    logger.LogInformation("Ongoing indexer has indexed the block {@context}", new
                    {
                        BlockchainId = BlockchainId,
                        BlockNumber = newBlock.Header.Number,
                        BlockId = newBlock.Header.Id
                    });

                    break;

                case MovementDirection.Backward:
                    await coinsBlockCanceler.Cancel(chainWalkerMovement.EvictedBlockHeader);

                    // This is needed to mitigate events publishing latency
                    indexingResult.AddBackgroundTask(
                        publisher.Publish(new BlockCancelled
                        {
                            BlockchainId = BlockchainId,
                            BlockId = chainWalkerMovement.EvictedBlockHeader.Id,
                            BlockNumber = chainWalkerMovement.EvictedBlockHeader.Number,
                            ChainSequence = Sequence
                        }));
                    
                    NextBlock--;
                    Sequence++;

                    logger.LogWarning("Ongoing indexer has reverted the block {@context}", new
                    {
                        BlockchainId = BlockchainId,
                        BlockNumber = chainWalkerMovement.EvictedBlockHeader.Number,
                        BlockId = chainWalkerMovement.EvictedBlockHeader.Id
                    });

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(chainWalkerMovement.Direction), chainWalkerMovement.Direction, string.Empty);
            }

            return indexingResult;
        }
    }
}
