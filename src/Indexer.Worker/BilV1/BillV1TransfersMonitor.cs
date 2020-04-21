using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.ObservedOperations;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.ObservedOperations;
using Lykke.Service.BlockchainApi.Client.Models;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.MessagingContract;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Worker.BilV1
{
    public class BillV1TransfersMonitor
    {
        private readonly ILogger<BillV1TransfersMonitor> _logger;
        private readonly BilV1ApiClientProvider _apiClientProvider;
        private readonly IObservedOperationsRepository _observedOperationsRepository;
        private readonly IAssetsRepository _assetsRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public BillV1TransfersMonitor(ILogger<BillV1TransfersMonitor> logger,
            BilV1ApiClientProvider apiClientProvider,
            IObservedOperationsRepository observedOperationsRepository,
            IAssetsRepository assetsRepository,
            IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _apiClientProvider = apiClientProvider;
            _observedOperationsRepository = observedOperationsRepository;
            _assetsRepository = assetsRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task ProcessAsync()
        {
            var cursor = default(long?);

            do
            {
                var observedOperations = await _observedOperationsRepository.GetExecutingAsync(cursor, 1000);
                
                if (!observedOperations.Any())
                {
                    break;
                }

                cursor = observedOperations.Last().OperationId;

                var tasks = new List<Task>();
                var updatedOperations = new ConcurrentBag<ObservedOperation>();

                foreach (var blockchainTransfers in observedOperations
                    .Where(x => !x.IsCompleted)
                    .GroupBy(x => x.BlockchainId))
                {
                    var task = Task.Factory
                        .StartNew(() => ProcessBlockchainTransfers(blockchainTransfers, updatedOperations).GetAwaiter().GetResult());

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                await _observedOperationsRepository.UpdateBatchAsync(updatedOperations);

                // TODO: For idempotency we need outbox here
                foreach (var operation in updatedOperations)
                {
                    foreach (var evt in operation.Events)
                    {
                        await _publishEndpoint.Publish(evt);
                    }
                }

                //TODO: Remove in V2 
                //foreach (var operation in updatedOperations)
                //{
                //    var apiClient = await _apiClientProvider.GetAsync(operation.BlockchainId);

                //    await apiClient.ForgetBroadcastedTransactionsAsync(operation.BilV1OperationId);
                //}

                await Task.Delay(25_000);
            } while (true);
        }

        private async Task ProcessBlockchainTransfers(
            IGrouping<string, 
                ObservedOperation> observedOperations, 
            ConcurrentBag<ObservedOperation> updatedOperations)
        {
            var apiClient = await _apiClientProvider.GetAsync(observedOperations.Key);

            try
            {
                foreach (var operation in observedOperations)
                {
                    // TODO: Add caching decorator for the asset repo
                    var asset = await _assetsRepository.GetAsync(operation.AssetId);

                    var transaction = await apiClient.GetBroadcastedSingleTransactionAsync(operation.BilV1OperationId,
                        new BlockchainAsset(new AssetContract
                        {
                            AssetId = asset.Symbol,
                            Address = asset.Address,
                            Accuracy = asset.Accuracy,
                            Name = asset.Symbol
                        }));

                    if (transaction == null)
                    {
                        operation.Complete(0, null);

                        updatedOperations.Add(operation);

                        _logger.LogWarning(
                            "BIL v1 API has returned no transaction. Assuming, it was cleaned already. Transfer has been completed {@context}",
                            new {Transfer = operation});
                        
                        continue;
                    }

                    switch (transaction.State)
                    {
                        case BroadcastedTransactionState.InProgress:
                            continue;

                        case BroadcastedTransactionState.Completed:
                            operation.Complete(transaction.Block, new[] {new Unit(operation.AssetId, transaction.Fee)});
                            updatedOperations.Add(operation);

                            _logger.LogInformation("Transfer has been completed {@context}", new {Transfer = operation});
                            
                            break;

                        case BroadcastedTransactionState.Failed:
                            operation.Fail(
                                transaction.Block,
                                new[] {new Unit(operation.AssetId, transaction.Fee)},
                                new TransactionError()
                                {
                                    Code = transaction.ErrorCode switch
                                    {
                                        BlockchainErrorCode.Unknown => TransactionErrorCode.Unknown,
                                        BlockchainErrorCode.AmountIsTooSmall => TransactionErrorCode.Unknown,
                                        BlockchainErrorCode.NotEnoughBalance => TransactionErrorCode.NotEnoughBalance,
                                        BlockchainErrorCode.BuildingShouldBeRepeated => TransactionErrorCode.Unknown,
                                        null => TransactionErrorCode.Unknown,
                                        _ => throw new ArgumentOutOfRangeException(nameof(transaction.ErrorCode), transaction.ErrorCode, "")
                                    },
                                    Message = transaction.Error
                                });
                            updatedOperations.Add(operation);

                            _logger.LogInformation("Transfer has been failed {@context}", new {Transfer = operation});

                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(transaction.State), transaction.State, "");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process executing transfers {@context}",
                    new {BlockchainId = observedOperations.Key});
            }
        }
    }
}
