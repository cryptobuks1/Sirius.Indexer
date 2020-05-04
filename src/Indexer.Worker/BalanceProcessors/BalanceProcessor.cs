using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Indexer.Bilv1.Domain.Models.EnrolledBalances;
using Indexer.Bilv1.Domain.Repositories;
using Lykke.Service.BlockchainApi.Client;
using Lykke.Service.BlockchainApi.Client.Models;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.MessagingContract;
using Swisschain.Sirius.Sdk.Primitives;
using BlockchainAsset = Lykke.Service.BlockchainApi.Client.Models.BlockchainAsset;

namespace Indexer.Worker.BalanceProcessors
{
    public class BalanceProcessor
    {
        private readonly string _blockchainId;
        private readonly ILogger<BalanceProcessor> _logger;
        private readonly IBlockchainApiClient _blockchainApiClient;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;

        private readonly IReadOnlyDictionary<string, (string BlockchainId, long BlockchainAssetId)> _assets =
            new ReadOnlyDictionary<string, (string BlockchainId, long BlockchainAssetId)>(
                new Dictionary<string, (string BlockchainId, long BlockchainAssetId)>()
                {
                    {"bitcoin-regtest", ("bitcoin-regtest", 100_000)},
                    {"ethereum-ropsten", ("ethereum-ropsten", 100_001)}
                });

        private IReadOnlyDictionary<string, BlockchainAsset> _blockchainAssets;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IOperationRepository _operationRepository;
        
        public BalanceProcessor(string blockchainId,
            ILogger<BalanceProcessor> logger,
            IBlockchainApiClient blockchainApiClient,
            IEnrolledBalanceRepository enrolledBalanceRepository,
            IOperationRepository operationRepository,
            IReadOnlyDictionary<string, BlockchainAsset> blockchainAssets,
            IPublishEndpoint publishEndpoint)
        {
            _blockchainId = blockchainId;
            _logger = logger;
            _blockchainApiClient = blockchainApiClient;
            _enrolledBalanceRepository = enrolledBalanceRepository;
            _blockchainAssets = blockchainAssets;
            _publishEndpoint = publishEndpoint;
            _operationRepository = operationRepository;
        }

        public async Task ProcessAsync(int batchSize)
        {
            var skip = 0;
            var balancesFromDatabase = new List<EnrolledBalance>();
            do
            {
                var received = await _enrolledBalanceRepository.GetAllForBlockchainAsync(_blockchainId, skip, 100);

                if (received == null || !received.Any())
                    break;

                balancesFromDatabase.AddRange(received);

                skip += received.Count;

                if (received.Count < 100)
                    break;

            } while (true);

            var balancesFromApi = new Dictionary<(string AssetId, string Address), WalletBalance>();
            await _blockchainApiClient.EnumerateWalletBalanceBatchesAsync(
                batchSize,
                assetId => GetAssetAccuracy(assetId, batchSize),
                batch =>
                {
                    if (batch != null && batch.Any())
                    {
                        foreach (var item in batch)
                        {
                            balancesFromApi[(item.AssetId, item.Address)] = item;
                        }
                    }

                    return Task.FromResult(true);
                });

            await ProcessBalancesBatchAsync(balancesFromApi, balancesFromDatabase);
        }

        private async Task ProcessBalancesBatchAsync(
            IDictionary<(string AssetId, string Address), WalletBalance> fromApi,
            List<EnrolledBalance> fromDatabase)
        {
            foreach (var balance in fromDatabase)
            {
                await ProcessBalance(balance, fromApi);
            }
        }

        private async Task ProcessBalance(EnrolledBalance enrolledBalance,
            IDictionary<(string AssetId, string Address), WalletBalance> depositWallets)
        {
            var key = enrolledBalance.Key;

            if (!_blockchainAssets.TryGetValue(key.BlockchainAssetId, out var blockchainAsset))
            {
                _logger.LogWarning("Blockchain asset is not found {depositWallet}}", enrolledBalance);

                return;
            }

            depositWallets.TryGetValue(
                (enrolledBalance.Key.BlockchainAssetId,
                    enrolledBalance.Key.WalletAddress),
                out var depositWallet);


            var balance = depositWallet?.Balance ?? 0m;
            var balanceBlock = depositWallet?.Block ?? enrolledBalance.Block;

            var cashinCouldBeStarted = CouldBeStarted(
                balance,
                balanceBlock,
                enrolledBalance.Balance,
                enrolledBalance.Block,
                out var operationAmount);

            if (!cashinCouldBeStarted)
            {
                return;
            }

            _assets.TryGetValue(_blockchainId, out var blockchainMapped);

            var operation = await _operationRepository.AddAsync(key, operationAmount, enrolledBalance.Block);

            if (operationAmount > 0)
            {
                var detectedTransaction = new TransactionDetected()
                {
                    BlockchainId = blockchainMapped.BlockchainId,
                    BlockId = "some block",
                    BlockNumber = balanceBlock,
                    Fees =new List<Unit>(),
                    TransactionId = operation.OperationId.ToString(),
                    TransactionNumber = 0,
                    Error = null,
                    Sources = Array.Empty<TransferSource>(),
                    OperationId = null,
                    Destinations = new List<TransferDestination>()
                    {
                        new TransferDestination()
                        {
                            TransferId = "0",
                            Address = key.WalletAddress,
                            Unit = new Unit(blockchainMapped.BlockchainAssetId, operationAmount)
                        }
                    }
                };

                await _publishEndpoint.Publish(detectedTransaction);
            }
            else
            {
                // TODO: Withdrawal
            }

            await _enrolledBalanceRepository.SetBalanceAsync(
                key,
                balance,
                balanceBlock);
        }


        private static bool CouldBeStarted(
            decimal balanceAmount,
            BigInteger balanceBlock,
            decimal enrolledBalanceAmount,
            BigInteger enrolledBalanceBlock,
            out decimal operationAmount)
        {
            operationAmount = 0;

            if (balanceBlock < enrolledBalanceBlock)
            {
                // This balance was already processed
                return false;
            }

            operationAmount = balanceAmount - enrolledBalanceAmount;

            if (operationAmount == 0)
            {
                // No visbible changes have happened since the last check
                return false;
            }

            return true;
        }

        private async Task<IReadOnlyDictionary<string, EnrolledBalance>> GetEnrolledBalancesAsync(IEnumerable<WalletBalance>
        balances)
        {
            var walletKeys = balances.Select(x => new DepositWalletKey(
                    blockchainAssetId: x.AssetId,
                    blockchainId: _blockchainId,
                    depositWalletAddress: x.Address
                ));

            return (await _enrolledBalanceRepository.GetAsync(walletKeys))
            .ToDictionary(x => GetEnrolledBalancesDictionaryKey(x.Key.WalletAddress, x.Key.BlockchainAssetId),
        y => y);
        }

        private int GetAssetAccuracy(string assetId, int batchSize)
        {
            if (!_blockchainAssets.TryGetValue(assetId, out var asset))
            {
                // Unknown asset, tries to refresh cached assets

                _blockchainAssets = _blockchainApiClient
                    .GetAllAssetsAsync(batchSize)
                    .GetAwaiter()
                    .GetResult();

                if (!_blockchainAssets.TryGetValue(assetId, out asset))
                {
                    throw new InvalidOperationException($"Asset {assetId} not found");
                }
            }

            return asset.Accuracy;
        }

        private string GetEnrolledBalancesDictionaryKey(string address, string assetId)
        {
            return $"{address.ToLower(CultureInfo.InvariantCulture)}:{assetId}";
        }
    }

}

