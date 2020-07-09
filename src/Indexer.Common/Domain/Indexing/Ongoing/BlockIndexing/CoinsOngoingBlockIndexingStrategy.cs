using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers.Coins;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.ObservedOperations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.MessagingContract;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    internal sealed class CoinsOngoingBlockIndexingStrategy : IOngoingBlockIndexingStrategy
    {
        private readonly ILogger<CoinsOngoingBlockIndexingStrategy> _logger;
        private readonly CoinsBlock _block;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly UnspentCoinsFactory _unspentCoinsFactory;
        private readonly IPublishEndpoint _publisher;

        public CoinsOngoingBlockIndexingStrategy(ILogger<CoinsOngoingBlockIndexingStrategy> logger,
            CoinsBlock block, 
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            UnspentCoinsFactory unspentCoinsFactory,
            IPublishEndpoint publisher)
        {
            _logger = logger;
            _block = block;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _unspentCoinsFactory = unspentCoinsFactory;
            _publisher = publisher;
        }

        public bool IsBlockFound => _block != null;
        public BlockHeader BlockHeader => _block.Header;
        
        public async Task ApplyBlock(OngoingIndexer indexer)
        {
            await using var unitOfWork = await _blockchainDbUnitOfWorkFactory.StartTransactional(_block.Header.BlockchainId);

            try
            {
                await ApplyBlock(indexer, unitOfWork);
                await unitOfWork.Commit();
            }
            catch
            {
                await unitOfWork.Rollback();
                throw;
            }
        }
        
        private async Task ApplyBlock(OngoingIndexer indexer, 
            ITransactionalBlockchainDbUnitOfWork unitOfWork)
        {
            var inputCoins = _block.Transfers.SelectMany(x => x.InputCoins).ToArray();

            await unitOfWork.InputCoins.InsertOrIgnore(inputCoins);

            var outputCoins = await _unspentCoinsFactory.Create(_block.Transfers);

            await unitOfWork.UnspentCoins.InsertOrIgnore(outputCoins);
            await unitOfWork.TransactionHeaders.InsertOrIgnore(_block.Transfers.Select(x => x.Header).ToArray());
            await unitOfWork.BlockHeaders.InsertOrIgnore(_block.Header);

            var inputsToSpend = inputCoins
                .Where(x => x.Type == InputCoinType.Regular)
                .ToDictionary(x => x.PreviousOutput);

            var coinsToSpend = await unitOfWork.UnspentCoins.GetAnyOf(inputsToSpend.Keys);

            if (inputsToSpend.Count != coinsToSpend.Count && coinsToSpend.Count != 0)
            {
                _logger.LogWarning("Not all unspent coins found for the given inputs to spend. History is missed for this inputs. Fees and balances can be incorrect for this block {@context}", new
                {
                    BlockchainId = indexer.BlockchainId,
                    BlockId = _block.Header.Id,
                    BlockNumber = _block.Header.Number,
                    InputsCount = inputsToSpend.Count,
                    UnspentCoinsCount = coinsToSpend.Count
                });
            }

            var spentByBlockCoins = coinsToSpend.Select(x => x.Spend(inputsToSpend[x.Id])).ToArray();

            //TODO: insert into xx from select u.* from unspent_coins, input_coins, transaction_headers...
            await unitOfWork.SpentCoins.InsertOrIgnore(spentByBlockCoins);

            var balanceUpdates = CoinsBalanceUpdatesCalculator.Calculate(
                _block.Header,
                outputCoins,
                spentByBlockCoins);

            await unitOfWork.BalanceUpdates.InsertOrIgnore(balanceUpdates);

            var fees = CoinsFeesCalculator.Calculate(
                _block.Header,
                outputCoins,
                spentByBlockCoins);

            await unitOfWork.Fees.InsertOrIgnore(fees);

            await PublishBlockEvents(
                unitOfWork.ObservedOperations,
                indexer,
                _block,
                spentByBlockCoins,
                outputCoins,
                fees);

            // TODO: delete from xx (select u.* from unspent_coins, input_coins, transaction_headers...
            await unitOfWork.UnspentCoins.Remove(spentByBlockCoins.Select(x => x.Id).ToArray());
        }
        
        private async Task PublishBlockEvents(IObservedOperationsRepository observedOperationsRepository,
            OngoingIndexer indexer,
            CoinsBlock block,
            IReadOnlyCollection<SpentCoin> spentByBlockCoins,
            IReadOnlyCollection<UnspentCoin> outputCoins,
            IReadOnlyCollection<Fee> fees)
        {
            var observedOperations = (await observedOperationsRepository.GetInvolvedInBlock(block.Header.Id))
                .ToDictionary(x => x.TransactionId);

            // This is needed to mitigate events publishing latency
            var tasks = new List<Task>(1 + block.Transfers.Count)
            {
                _publisher.Publish(new BlockDetected
                {
                    BlockchainId = indexer.BlockchainId,
                    BlockId = block.Header.Id,
                    BlockNumber = block.Header.Number,
                    MinedAt = block.Header.MinedAt,
                    PreviousBlockId = block.Header.PreviousId,
                    ChainSequence = indexer.Sequence
                })
            };
            
            var spentCoinsByTransaction = spentByBlockCoins.ToLookup(x => x.SpentByCoinId.TransactionId);
            var outputCoinsByTransaction = outputCoins.ToLookup(x => x.Id.TransactionId);
            var feesByTransaction = fees.ToLookup(x => x.TransactionId);

            foreach (var transfer in block.Transfers)
            {
                tasks.Add(
                    _publisher.Publish(new TransactionDetected
                    {
                        BlockchainId = indexer.BlockchainId,
                        BlockId = block.Header.Id,
                        BlockNumber = block.Header.Number,
                        BlockMinedAt = block.Header.MinedAt,
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
                                Unit = x.Unit
                            })
                            .ToArray(),
                        Destinations = outputCoinsByTransaction[transfer.Header.Id]
                            .Select(x => new TransferDestination
                            {
                                Address = x.Address,
                                Unit = x.Unit,
                                TagType = x.TagType,
                                Tag = x.Tag
                            })
                            .ToArray()
                    }));
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation("The block has been indexed {@context}",
                new
                {
                    BlockchainId = indexer.BlockchainId,
                    BlockNumber = block.Header.Number,
                    BlockId = block.Header.Id,
                    TransfersCount = block.Transfers.Count,
                    ChainSequence = indexer.Sequence
                });
        }

    }
}
