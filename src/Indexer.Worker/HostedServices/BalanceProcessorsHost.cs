using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Bilv1.Domain.Repositories;
using Indexer.Common.Bilv1.DomainServices;
using Indexer.Common.Persistence;
using Indexer.Worker.BalanceProcessors;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.HostedServices
{
    public class BalanceProcessorsHost : IHostedService, IDisposable
    {
        private readonly ILogger<BalanceProcessorsHost> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly BlockchainApiClientProvider _blockchainApiClientProvider;
        private readonly IEnrolledBalanceRepository _enrolledBalanceRepository;
        private readonly IOperationRepository _operationRepository;
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly List<BalanceProcessorJob> _balanceReaders;

        public BalanceProcessorsHost(
            ILogger<BalanceProcessorsHost> logger,
            ILoggerFactory loggerFactory,
            BlockchainApiClientProvider blockchainApiClientProvider,
            IEnrolledBalanceRepository enrolledBalanceRepository,
            IOperationRepository operationRepository,
            IBlockchainsRepository blockchainsRepository,
            IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _blockchainApiClientProvider = blockchainApiClientProvider;
            _enrolledBalanceRepository = enrolledBalanceRepository;
            _operationRepository = operationRepository;
            _blockchainsRepository = blockchainsRepository;
            _publishEndpoint = publishEndpoint;

            _balanceReaders = new List<BalanceProcessorJob>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                foreach (var blockchain in await _blockchainsRepository.GetAllAsync())
                {
                    try
                    {
                        var blockchainApiClient = await _blockchainApiClientProvider.GetAsync(blockchain.Id);
                        var blockchainAssetsDict = await blockchainApiClient.GetAllAssetsAsync(100);

                        var balanceProcessor = new BalanceProcessor(
                            blockchain.Id,
                            _loggerFactory.CreateLogger<BalanceProcessor>(),
                            blockchainApiClient,
                            _enrolledBalanceRepository,
                            _operationRepository,
                            blockchainAssetsDict,
                            _publishEndpoint);

                        var balanceReader = new BalanceProcessorJob(
                            blockchain.Id,
                            _loggerFactory.CreateLogger<BalanceProcessorsHost>(),
                            balanceProcessor,
                            TimeSpan.FromSeconds(10));

                        balanceReader.Start();

                        _balanceReaders.Add(balanceReader);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to initialize balances processor {@context}",
                            new
                            {
                                BlockchainId = blockchain.Id,
                                IntegrationUrl = blockchain.IntegrationUrl
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process balances");

                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var balanceReader in _balanceReaders)
            {
                balanceReader.Stop();
            }

            foreach (var balanceReader in _balanceReaders)
            {
                balanceReader.Wait();
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var balanceReader in _balanceReaders)
            {
                balanceReader.Dispose();
            }
        }
    }
}
