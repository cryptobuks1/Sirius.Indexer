using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Domain.Indexing.Common.CoinBlocks;
using Indexer.Common.Persistence.Entities.ObservedOperations;
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

        public async Task<OngoingBlockIndexingResult> IndexNextBlock(ILogger<OngoingIndexer> logger,
            IBlocksReader reader,
            ChainWalker chainWalker,
            PrimaryBlockProcessor primaryBlockProcessor,
            CoinsPrimaryBlockProcessor coinsPrimaryBlockProcessor,
            CoinsSecondaryBlockProcessor coinsSecondaryBlockProcessor,
            CoinsBlockCanceler coinsBlockCanceler,
            IObservedOperationsRepository observedOperationsRepository,
            IPublishEndpoint publisher)
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
                    await MoveForward(logger,
                        primaryBlockProcessor, 
                        coinsPrimaryBlockProcessor,
                        coinsSecondaryBlockProcessor,
                        observedOperationsRepository,
                        publisher,
                        newBlock);

                    break;

                case MovementDirection.Backward:
                    await MoveBackward(logger, 
                        coinsBlockCanceler, 
                        publisher,
                        chainWalkerMovement.EvictedBlockHeader);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(chainWalkerMovement.Direction), chainWalkerMovement.Direction, string.Empty);
            }

            return OngoingBlockIndexingResult.BlockIndexed;
        }

        private async Task MoveForward(ILogger<OngoingIndexer> logger,
            PrimaryBlockProcessor primaryBlockProcessor,
            CoinsPrimaryBlockProcessor coinsPrimaryBlockProcessor,
            CoinsSecondaryBlockProcessor coinsSecondaryBlockProcessor,
            IObservedOperationsRepository observedOperationsRepository,
            IPublishEndpoint publisher,
            CoinsBlock newBlock)
        {
            await primaryBlockProcessor.Process(newBlock.Header, newBlock.Transfers.Select(x => x.Header).ToArray());

            var primaryBlockProcessingResult = await coinsPrimaryBlockProcessor.Process(newBlock);
            var secondaryBlockGeneratingPhaseProcessingResult = await coinsSecondaryBlockProcessor.ProcessGeneratingPhase(
                newBlock.Header, 
                primaryBlockProcessingResult.UnspentCoins);
            
            await PublishBlockEvents(
                observedOperationsRepository, 
                publisher, 
                newBlock,
                primaryBlockProcessingResult,
                secondaryBlockGeneratingPhaseProcessingResult);

            await coinsSecondaryBlockProcessor.ProcessRemovingPhase(
                newBlock.Header,
                secondaryBlockGeneratingPhaseProcessingResult);

            NextBlock++;
            Sequence++;

            logger.LogInformation("Ongoing indexer has indexed the block {@context}",
                new
                {
                    BlockchainId = BlockchainId,
                    BlockNumber = newBlock.Header.Number,
                    BlockId = newBlock.Header.Id,
                    TransfersCount = newBlock.Transfers.Count
                });
        }

        private async Task PublishBlockEvents(IObservedOperationsRepository observedOperationsRepository,
            IPublishEndpoint publisher,
            CoinsBlock newBlock,
            CoinsPrimaryBlockProcessingResult primaryBlockProcessingResult,
            CoinsSecondaryBlockGenerationPhaseProcessingResult secondaryBlockGeneratingPhaseProcessingResult)
        {
            var observedOperations = (await observedOperationsRepository.GetInvolvedInBlock(BlockchainId, newBlock.Header.Id))
                .ToDictionary(x => x.TransactionId);

            // This is needed to mitigate events publishing latency
            var tasks = new List<Task>(1 + newBlock.Transfers.Count)
            {
                publisher.Publish(new BlockDetected
                {
                    BlockchainId = BlockchainId,
                    BlockId = newBlock.Header.Id,
                    BlockNumber = newBlock.Header.Number,
                    PreviousBlockId = newBlock.Header.PreviousId,
                    ChainSequence = Sequence
                })
            };
            
            var spentCoinsByTransaction = secondaryBlockGeneratingPhaseProcessingResult.SpentCoins.ToLookup(x => x.SpentByCoinId.TransactionId);
            var unspentCoinsByTransaction = primaryBlockProcessingResult.UnspentCoins.ToLookup(x => x.Id.TransactionId);
            var feesByTransaction = secondaryBlockGeneratingPhaseProcessingResult.Fees.ToLookup(x => x.TransactionId);

            foreach (var transfer in newBlock.Transfers)
            {
                tasks.Add(
                    publisher.Publish(new TransactionDetected
                    {
                        BlockchainId = BlockchainId,
                        BlockId = newBlock.Header.Id,
                        BlockNumber = newBlock.Header.Number,
                        TransactionId = transfer.Header.Id,
                        TransactionNumber = transfer.Header.Number,
                        Error = transfer.Header.Error,
                        OperationId = observedOperations.TryGetValue(transfer.Header.Id, out var operation)
                            ? operation.Id
                            : default(long?),
                        Fees = feesByTransaction[transfer.Header.Id].Select(x => x.Unit).ToArray(),
                        Sources = spentCoinsByTransaction[transfer.Header.Id]
                            .Select(x => new TransferSource
                            {
                                Address = x.Address,
                                Unit = x.Unit,
                                SpentCoin = x.Id,
                                TransferId = $"input-{x.SpentByCoinId.Number.ToString()}"
                            })
                            .ToArray(),
                        Destinations = unspentCoinsByTransaction[transfer.Header.Id]
                            .Select(x => new TransferDestination
                            {
                                Address = x.Address,
                                Unit = x.Unit,
                                TagType = x.TagType,
                                Tag = x.Tag,
                                CoinNumber = x.Id.Number,
                                TransferId = $"output-{x.Id.Number.ToString()}"
                            })
                            .ToArray()
                    }));
            }

            await Task.WhenAll(tasks);
        }

        private async Task MoveBackward(ILogger<OngoingIndexer> logger,
            CoinsBlockCanceler coinsBlockCanceler,
            IPublishEndpoint publisher,
            BlockHeader evictedBlockHeader)
        {
            await coinsBlockCanceler.Cancel(evictedBlockHeader);

            await publisher.Publish(new BlockCancelled
            {
                BlockchainId = BlockchainId,
                BlockId = evictedBlockHeader.Id,
                BlockNumber = evictedBlockHeader.Number,
                ChainSequence = Sequence
            });

            NextBlock--;
            Sequence++;

            logger.LogWarning("Ongoing indexer has reverted the block {@context}",
                new
                {
                    BlockchainId = BlockchainId,
                    BlockNumber = evictedBlockHeader.Number,
                    BlockId = evictedBlockHeader.Id
                });
        }
    }
}
