using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.ObservedOperations;
using MassTransit;
using Swisschain.Sirius.Indexer.MessagingContract;
using Swisschain.Sirius.Sdk.Primitives;
using TransferDestination = Swisschain.Sirius.Indexer.MessagingContract.TransferDestination;
using TransferSource = Swisschain.Sirius.Indexer.MessagingContract.TransferSource;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    internal sealed class NonceOngoingBlockIndexingStrategy : IOngoingBlockIndexingStrategy
    {
        private readonly NonceBlock _block;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly NonceBlockAssetsProvider _blockAssetsProvider;
        private readonly IPublishEndpoint _publisher;

        public NonceOngoingBlockIndexingStrategy(NonceBlock block,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            NonceBlockAssetsProvider blockAssetsProvider,
            IPublishEndpoint publisher)
        {
            _block = block;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _blockAssetsProvider = blockAssetsProvider;
            _publisher = publisher;
        }

        public bool IsBlockFound => _block != null;
        public BlockHeader BlockHeader => _block.Header;
        public int TransfersCount => _block.Transfers.Count;

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

        public async Task ApplyBlock(OngoingIndexer indexer, IBlockchainDbUnitOfWork unitOfWork)
        {
            var nonceUpdates = _block.Transfers
                .SelectMany(tx => tx.NonceUpdates)
                .GroupBy(x => new
                {
                    x.Address,
                    x.BlockId
                })
                .Select(g => new NonceUpdate(
                    g.Key.Address,
                    g.Key.BlockId,
                    g.Max(x => x.Nonce)))
                .ToArray();

            // TODO: Save operations

            await unitOfWork.NonceUpdates.InsertOrIgnore(nonceUpdates);

            var operations = _block.Transfers.SelectMany(tx => tx.Operations).ToArray();
            var blockSources = operations.SelectMany(x => x.Sources).ToArray();
            var blockDestinations = operations.SelectMany(x => x.Destinations).ToArray();
            var blockFeeSources = _block.Transfers.SelectMany(x => x.Fees).ToArray();
            
            var blockAssets = await _blockAssetsProvider.Get(
                _block.Header.BlockchainId,
                blockSources,
                blockDestinations,
                blockFeeSources);
            
            var fees = NonceFeesFactory.Create(_block.Transfers, blockAssets);

            await unitOfWork.Fees.InsertOrIgnore(fees);

            var balanceUpdates = NonceBalanceUpdatesCalculator.Calculate(
                _block.Header,
                blockSources,
                blockDestinations,
                blockFeeSources,
                blockAssets);

            await unitOfWork.BalanceUpdates.InsertOrIgnore(balanceUpdates);
            await unitOfWork.TransactionHeaders.InsertOrIgnore(_block.Transfers.Select(x => x.Header).ToArray());
            await unitOfWork.BlockHeaders.InsertOrIgnore(_block.Header);

            await PublishBlockEvents(
                unitOfWork.ObservedOperations, 
                indexer,
                blockAssets);
        }

        private async Task PublishBlockEvents(IObservedOperationsRepository observedOperationsRepository,
            OngoingIndexer indexer,
            IReadOnlyDictionary<BlockchainAssetId, Asset> blockAssets)
        {
            var observedOperations = (await observedOperationsRepository.GetInvolvedInBlock(_block.Header.Id))
                .ToDictionary(x => x.TransactionId);

            // This is needed to mitigate events publishing latency
            var tasks = new List<Task>(1 + _block.Transfers.Count)
            {
                _publisher.Publish(new BlockDetected
                {
                    BlockchainId = _block.Header.BlockchainId,
                    BlockId = _block.Header.Id,
                    BlockNumber = _block.Header.Number,
                    MinedAt = _block.Header.MinedAt,
                    PreviousBlockId = _block.Header.PreviousId,
                    ChainSequence = indexer.Sequence
                })
            };

            foreach (var transfer in _block.Transfers)
            {
                var operationSources = transfer.Operations.SelectMany(x => x.Sources);
                var feeSources = transfer.Fees;
                
                var allSources = operationSources
                    .Select(x => new TransferSource
                    {
                        Address = x.Sender.Address,
                        Unit = new Unit(blockAssets[x.Unit.Asset.Id].Id, x.Unit.Amount)
                    })
                    .Concat(feeSources
                        .Select(x => new TransferSource
                        {
                            Address = x.FeePayerAddress,
                            Unit = new Unit(blockAssets[x.BlockchainUnit.Asset.Id].Id, x.BlockchainUnit.Amount)
                        }))
                    .ToArray();
                
                var operationDestinations = transfer.Operations
                    .SelectMany(x => x.Destinations)
                    .Select(x => new TransferDestination
                    {
                        Address = x.Recipient.Address,
                        Tag = x.Recipient.Tag,
                        TagType = x.Recipient.TagType,
                        Unit = new Unit(blockAssets[x.Unit.Asset.Id].Id, x.Unit.Amount)
                    })
                    .ToArray();

                var fees = feeSources
                    .Select(x => new Unit(blockAssets[x.BlockchainUnit.Asset.Id].Id, x.BlockchainUnit.Amount))
                    .ToArray();

                tasks.Add(
                    _publisher.Publish(new TransactionDetected
                    {
                        BlockchainId = _block.Header.BlockchainId,
                        BlockId = _block.Header.Id,
                        BlockNumber = _block.Header.Number,
                        BlockMinedAt = _block.Header.MinedAt,
                        TransactionId = transfer.Header.Id,
                        TransactionNumber = transfer.Header.Number,
                        Error = transfer.Header.Error,
                        OperationId = observedOperations.TryGetValue(transfer.Header.Id, out var operation)
                            ? operation.Id
                            : default(long?),
                        Fees = fees,
                        Sources = allSources,
                        Destinations = operationDestinations
                    }));
            }

            await Task.WhenAll(tasks);
        }
    }
}
