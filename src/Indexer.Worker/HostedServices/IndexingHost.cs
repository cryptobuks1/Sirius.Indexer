using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.InMemoryBus;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Integrations.Client;

namespace Indexer.Worker.HostedServices
{
    public class IndexingHost : IHostedService
    {
        private readonly ILogger<IndexingHost> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppConfig _config;
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly IFirstPassIndexersRepository _firstPassIndexersRepository;
        private readonly BlocksProcessor _blocksProcessor;
        private readonly IInMemoryBus _inMemoryBus;
        private readonly List<FirstPassIndexingJob> _firstPassIndexingJobs;
        private readonly List<ISiriusIntegrationClient> _integrationClients;

        public IndexingHost(ILogger<IndexingHost> logger,
            ILoggerFactory loggerFactory,
            AppConfig config,
            IBlockchainsRepository blockchainsRepository,
            IFirstPassIndexersRepository firstPassIndexersRepository,
            BlocksProcessor blocksProcessor,
            IInMemoryBus inMemoryBus)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _config = config;
            _blockchainsRepository = blockchainsRepository;
            _firstPassIndexersRepository = firstPassIndexersRepository;
            _blocksProcessor = blocksProcessor;
            _inMemoryBus = inMemoryBus;

            _firstPassIndexingJobs = new List<FirstPassIndexingJob>();
            _integrationClients = new List<ISiriusIntegrationClient>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Indexing is being started...");

            foreach (var (blockchainId, blockchainConfig) in _config.Indexing.Blockchains)
            {
                var blockchainMetamodel = await _blockchainsRepository.GetAsync(blockchainId);
                var integrationClient = new SiriusIntegrationClient(blockchainMetamodel.IntegrationUrl, unencrypted: true);
                var blocksReader = new BlocksReader(
                    _loggerFactory.CreateLogger<BlocksReader>(),
                    integrationClient,
                    blockchainMetamodel);
                var firstPassIndexingJobs = Enumerable
                    .Range(0, blockchainConfig.FirstPassIndexersCount)
                    .Select(i =>
                    {
                        var startBlock = blockchainMetamodel.Protocol.StartBlockNumber + i * blockchainConfig.FirstPassIndexerLength;
                        var stopBlock = startBlock + blockchainConfig.FirstPassIndexerLength;
                        var isIrreversibleIndexer = i != blockchainConfig.FirstPassIndexersCount - 1;

                        return new FirstPassIndexingJob(
                            _loggerFactory.CreateLogger<FirstPassIndexingJob>(),
                            _loggerFactory,
                            new FirstPassIndexerId(blockchainId, startBlock),
                            isIrreversibleIndexer ? stopBlock : default(long?),
                            blockchainConfig.DelayOnBlockNotFound,
                            _firstPassIndexersRepository,
                            blocksReader,
                            _blocksProcessor,
                            _inMemoryBus);
                    });

                _firstPassIndexingJobs.AddRange(firstPassIndexingJobs);
                _integrationClients.Add(integrationClient);
            }

            foreach (var job in _firstPassIndexingJobs)
            {
                await job.Start();
            }

            _logger.LogInformation("Indexing has been started.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Indexing is being stopped...");

            foreach (var job in _firstPassIndexingJobs)
            {
                job.Stop();
            }

            foreach (var job in _firstPassIndexingJobs)
            {
                job.Wait();
            }

            foreach (var job in _firstPassIndexingJobs)
            {
                job.Dispose();
            }

            foreach (var client in _integrationClients.Cast<IDisposable>())
            {
                client.Dispose();
            }

            _logger.LogInformation("Indexing has been stopped.");

            return Task.CompletedTask;
        }
    }
}
