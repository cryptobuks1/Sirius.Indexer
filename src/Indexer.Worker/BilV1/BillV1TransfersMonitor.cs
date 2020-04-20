using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Lykke.Service.BlockchainApi.Client.Models;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Worker.BilV1
{
    public class BillV1TransfersMonitor
    {
        private readonly ILogger<BillV1TransfersMonitor> _logger;
        private readonly BilV1ApiClientProvider _apiClientProvider;
        private readonly ITransfersRepository _transfersRepository;
        private readonly IAssetsRepository _assetsRepository;
        private readonly IPublishEndpoint _publishEndpoint;

        public BillV1TransfersMonitor(ILogger<BillV1TransfersMonitor> logger,
            BilV1ApiClientProvider apiClientProvider,
            ITransfersRepository transfersRepository,
            IAssetsRepository assetsRepository,
            IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _apiClientProvider = apiClientProvider;
            _transfersRepository = transfersRepository;
            _assetsRepository = assetsRepository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task ProcessAsync()
        {
            var cursor = default(long?);

            do
            {
                var transfersPage = await _transfersRepository.GetExecutingAsync(cursor, 1000);
                
                if (!transfersPage.Any())
                {
                    break;
                }

                cursor = transfersPage.Last().Id;

                var tasks = new List<Task>();
                var updatedTransfers = new ConcurrentBag<Transfer>();

                foreach (var blockchainTransfers in transfersPage.Where(x => x.IsSent).GroupBy(x => x.BlockchainId))
                {
                    var task = Task.Factory.StartNew(() => ProcessBlockchainTransfers(blockchainTransfers, updatedTransfers).GetAwaiter().GetResult());

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);

                await _transfersRepository.UpdateBatchAsync(updatedTransfers);

                // TODO: For idempotency we need outbox here
                foreach (var transfer in updatedTransfers)
                {
                    foreach (var evt in transfer.Events)
                    {
                        await _publishEndpoint.Publish(evt);
                    }
                }

                foreach (var transfer in updatedTransfers)
                {
                    var apiClient = await _apiClientProvider.GetAsync(transfer.BlockchainId);

                    await apiClient.ForgetBroadcastedTransactionsAsync(transfer.BilV1OperationId);
                }

            } while (true);
        }

        private async Task ProcessBlockchainTransfers(IGrouping<string, Transfer> blockchainTransfers, ConcurrentBag<Transfer> updatedTransfers)
        {
            var apiClient = await _apiClientProvider.GetAsync(blockchainTransfers.Key);

            try
            {
                foreach (var transfer in blockchainTransfers)
                {
                    // TODO: Add caching decorator for the asset repo
                    var asset = await _assetsRepository.GetAsync(transfer.AssetId);

                    var transaction = await apiClient.GetBroadcastedSingleTransactionAsync(transfer.BilV1OperationId,
                        new BlockchainAsset(new AssetContract
                        {
                            AssetId = asset.Symbol,
                            Address = asset.Address,
                            Accuracy = asset.Accuracy,
                            Name = asset.Symbol
                        }));

                    if (transaction == null)
                    {
                        transfer.Complete(null);

                        updatedTransfers.Add(transfer);

                        _logger.LogWarning(
                            "BIL v1 API has returned no transaction. Assuming, it was cleaned already. Transfer has been completed {@context}",
                            new {Transfer = transfer});
                        
                        continue;
                    }

                    switch (transaction.State)
                    {
                        case BroadcastedTransactionState.InProgress:
                            continue;

                        case BroadcastedTransactionState.Completed:
                            transfer.Complete(new[] {new Unit(transfer.AssetId, transaction.Fee)});
                            updatedTransfers.Add(transfer);

                            _logger.LogInformation("Transfer has been completed {@context}", new {Transfer = transfer});
                            
                            break;

                        case BroadcastedTransactionState.Failed:
                            transfer.Fail(
                                new[] {new Unit(transfer.AssetId, transaction.Fee)},
                                new OperationError(
                                    transaction.Error,
                                    transaction.ErrorCode switch
                                    {
                                        BlockchainErrorCode.Unknown => OperationErrorCode.TechnicalProblem,
                                        BlockchainErrorCode.AmountIsTooSmall => OperationErrorCode.AmountIsTooSmall,
                                        BlockchainErrorCode.NotEnoughBalance => OperationErrorCode.NotEnoughBalance,
                                        BlockchainErrorCode.BuildingShouldBeRepeated => OperationErrorCode.TechnicalProblem,
                                        null => OperationErrorCode.TechnicalProblem,
                                        _ => throw new ArgumentOutOfRangeException(nameof(transaction.ErrorCode), transaction.ErrorCode, "")
                                    }));
                            updatedTransfers.Add(transfer);

                            _logger.LogInformation("Transfer has been failed {@context}", new {Transfer = transfer});

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
                    new {BlockchainId = blockchainTransfers.Key});
            }
        }
    }
}
