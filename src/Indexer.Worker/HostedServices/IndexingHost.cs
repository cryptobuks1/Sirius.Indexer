using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Messaging.InMemoryBus;
using Indexer.Common.Persistence;
using Indexer.Common.ReadModel.Blockchains;
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
        private readonly IFirstPassHistoryIndexersRepository _firstPassHistoryIndexersRepository;
        private readonly ISecondPassHistoryIndexersRepository _secondPassHistoryIndexersRepository;
        private readonly IBlocksRepository _blocksRepository;
        private readonly IInMemoryBus _inMemoryBus;
        private readonly List<FirstPassHistoryIndexingJob> _firstPassIndexingJobs;
        private readonly List<ISiriusIntegrationClient> _integrationClients;

        public IndexingHost(ILogger<IndexingHost> logger,
            ILoggerFactory loggerFactory,
            AppConfig config,
            IBlockchainsRepository blockchainsRepository,
            IFirstPassHistoryIndexersRepository firstPassHistoryIndexersRepository,
            ISecondPassHistoryIndexersRepository secondPassHistoryIndexersRepository,
            IBlocksRepository blocksRepository,
            IInMemoryBus inMemoryBus)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _config = config;
            _blockchainsRepository = blockchainsRepository;
            _firstPassHistoryIndexersRepository = firstPassHistoryIndexersRepository;
            _secondPassHistoryIndexersRepository = secondPassHistoryIndexersRepository;
            _blocksRepository = blocksRepository;
            _inMemoryBus = inMemoryBus;

            _firstPassIndexingJobs = new List<FirstPassHistoryIndexingJob>();
            _integrationClients = new List<ISiriusIntegrationClient>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Indexing is being started...");

            foreach (var (blockchainId, blockchainConfig) in _config.Indexing.Blockchains)
            {
                _logger.LogInformation(@"Blockchain indexing is being provisioned {@context}...",
                    new
                    {
                        BlockchainId = blockchainId,
                        BlockchainConfig = blockchainConfig
                    });

                var blockchainMetamodel = await _blockchainsRepository.GetAsync(blockchainId);

                await ProvisionBlockchainFirstPassHistoryIndexingJobs(blockchainId, blockchainConfig, blockchainMetamodel);
                await ProvisionBlockchainSecondPassHistoryIndexer(blockchainId, blockchainConfig, blockchainMetamodel);
            }

            foreach (var job in _firstPassIndexingJobs)
            {
                job.Start();
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

        private async Task ProvisionBlockchainSecondPassHistoryIndexer(string blockchainId, 
            BlockchainIndexingConfig blockchainConfig, 
            BlockchainMetamodel blockchainMetamodel)
        {
            var indexer = await _secondPassHistoryIndexersRepository.GetOrDefault(blockchainId);

            if (indexer == null)
            {
                _logger.LogInformation("Second-pass history indexer is being created {@context}", new
                {
                    BlockchainId = blockchainId,
                    StartBlock = blockchainMetamodel.Protocol.StartBlockNumber,
                    StopBlock = blockchainConfig.LastHistoricalBlockNumber
                });

                indexer = SecondPassHistoryIndexer.Create(
                    blockchainId, 
                    startBlock: blockchainMetamodel.Protocol.StartBlockNumber,
                    stopBlock: blockchainConfig.LastHistoricalBlockNumber);

                await _secondPassHistoryIndexersRepository.Add(indexer);
            }
            else
            {
                if (indexer.IsCompleted)
                {
                    _logger.LogInformation("Second-pass history indexer is already completed {@context}", new
                    {
                        BlockchainId = blockchainId,
                        StartBlock = blockchainMetamodel.Protocol.StartBlockNumber,
                        StopBlock = indexer.StopBlock,
                        NextBlock = indexer.NextBlock
                    });

                    // TODO: Start ongoing indexer
                }
                else
                {
                    _logger.LogInformation("Second-pass history indexer has been found {@context}", new
                    {
                        BlockchainId = blockchainId,
                        StartBlock = blockchainMetamodel.Protocol.StartBlockNumber,
                        StopBlock = indexer.StopBlock,
                        NextBlock = indexer.NextBlock
                    });
                }
            }
        }

        private async Task<bool> ProvisionBlockchainFirstPassHistoryIndexer(string blockchainId, long startBlock, long stopBlock)
        {
            var indexerId = new FirstPassHistoryIndexerId(blockchainId, startBlock);
            var indexer = await _firstPassHistoryIndexersRepository.GetOrDefault(indexerId);

            if (indexer == null)
            {
                _logger.LogInformation("First-pass history indexer is being created {@context}", new
                {
                    BlockchainId = indexerId.BlockchainId,
                    StartBlock = indexerId.StartBlock,
                    StopBlock = stopBlock
                });

                indexer = FirstPassHistoryIndexer.Start(indexerId, stopBlock);

                await _firstPassHistoryIndexersRepository.Add(indexer);
            }
            else
            {
                if (indexer.IsCompleted)
                {
                    _logger.LogInformation("First-pass history indexer is already completed {@context}", new
                    {
                        BlockchainId = indexerId.BlockchainId,
                        StartBlock = indexerId.StartBlock,
                        StopBlock = stopBlock,
                        NextBlock = indexer.NextBlock
                    });

                    return false;
                }

                _logger.LogInformation("First-pass history indexer has been found {@context}", new
                {
                    BlockchainId = indexerId.BlockchainId,
                    StartBlock = indexerId.StartBlock,
                    StopBlock = stopBlock,
                    NextBlock = indexer.NextBlock
                });
            }

            return true;
        }

        private async Task ProvisionBlockchainFirstPassHistoryIndexingJobs(string blockchainId,
            BlockchainIndexingConfig blockchainConfig,
            BlockchainMetamodel blockchainMetamodel)
        {
            var indexerBlocksCount = blockchainConfig.LastHistoricalBlockNumber /
                                     blockchainConfig.FirstPassHistoryIndexersCount;
            var integrationClient = new SiriusIntegrationClient(blockchainMetamodel.IntegrationUrl, unencrypted: true);
            var blocksReader = new BlocksReader(
                _loggerFactory.CreateLogger<BlocksReader>(),
                integrationClient,
                blockchainMetamodel);
            var jobFactoryTasks = Enumerable
                .Range(0, blockchainConfig.FirstPassHistoryIndexersCount)
                .Select(async i =>
                {
                    var startBlock = blockchainMetamodel.Protocol.StartBlockNumber + i * indexerBlocksCount;
                    var stopBlock = startBlock + indexerBlocksCount;
                    var isLastIndexer = i != blockchainConfig.FirstPassHistoryIndexersCount - 1;

                    if (await ProvisionBlockchainFirstPassHistoryIndexer(blockchainId, startBlock, stopBlock))
                    {
                        return new FirstPassHistoryIndexingJob(
                            _loggerFactory.CreateLogger<FirstPassHistoryIndexingJob>(),
                            _loggerFactory,
                            new FirstPassHistoryIndexerId(blockchainId, startBlock),
                            isLastIndexer ? stopBlock : blockchainConfig.LastHistoricalBlockNumber,
                            blockchainConfig.DelayOnBlockNotFound,
                            _firstPassHistoryIndexersRepository,
                            blocksReader,
                            _blocksRepository,
                            _inMemoryBus);
                    }

                    return null;
                })
                .ToArray();

            await Task.WhenAll(jobFactoryTasks);

            var jobs = jobFactoryTasks
                .Select(x => x.Result)
                .Where(x => x != null);

            _firstPassIndexingJobs.AddRange(jobs);
            _integrationClients.Add(integrationClient);
        }
    }
}
