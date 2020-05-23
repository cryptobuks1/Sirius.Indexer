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
using Indexer.Common.Telemetry;
using Indexer.Worker.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.HostedServices
{
    internal class IndexingHost : IHostedService, IDisposable
    {
        private readonly ILogger<IndexingHost> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppConfig _config;
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly IFirstPassHistoryIndexersRepository _firstPassHistoryIndexersRepository;
        private readonly ISecondPassHistoryIndexersRepository _secondPassHistoryIndexersRepository;
        private readonly IOngoingIndexersRepository _ongoingIndexersRepository;
        private readonly IBlocksRepository _blocksRepository;
        private readonly IInMemoryBus _inMemoryBus;
        private readonly SecondPassHistoryIndexingJobsManager _secondPassHistoryIndexingJobsManager;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IBlockReadersProvider _blockReadersProvider;
        private readonly IAppInsight _appInsight;
        private readonly List<FirstPassHistoryIndexingJob> _firstPassIndexingJobs;

        public IndexingHost(ILogger<IndexingHost> logger,
            ILoggerFactory loggerFactory,
            AppConfig config,
            IBlockchainsRepository blockchainsRepository,
            IFirstPassHistoryIndexersRepository firstPassHistoryIndexersRepository,
            ISecondPassHistoryIndexersRepository secondPassHistoryIndexersRepository,
            IOngoingIndexersRepository ongoingIndexersRepository,
            IBlocksRepository blocksRepository,
            IInMemoryBus inMemoryBus,
            SecondPassHistoryIndexingJobsManager secondPassHistoryIndexingJobsManager,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IBlockReadersProvider blockReadersProvider,
            IAppInsight appInsight)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _config = config;
            _blockchainsRepository = blockchainsRepository;
            _firstPassHistoryIndexersRepository = firstPassHistoryIndexersRepository;
            _secondPassHistoryIndexersRepository = secondPassHistoryIndexersRepository;
            _ongoingIndexersRepository = ongoingIndexersRepository;
            _blocksRepository = blocksRepository;
            _inMemoryBus = inMemoryBus;
            _secondPassHistoryIndexingJobsManager = secondPassHistoryIndexingJobsManager;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _blockReadersProvider = blockReadersProvider;
            _appInsight = appInsight;

            _firstPassIndexingJobs = new List<FirstPassHistoryIndexingJob>();
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
                var blocksReader = await _blockReadersProvider.Get(blockchainId);

                var firstPassIndexers = await ProvisionFirstPassHistoryIndexers(blockchainId, blockchainConfig, blockchainMetamodel);
                var secondPassIndexer = await ProvisionSecondPassHistoryIndexer(blockchainId, blockchainConfig, blockchainMetamodel);
                var ongoingIndexer = await ProvisionOngoingIndexer(blockchainId, blockchainConfig, blockchainMetamodel);
                
                await StartFirstPassHistoryIndexingJobs(firstPassIndexers, blocksReader);
                await StartSecondPassHistoryIndexingJob(firstPassIndexers, secondPassIndexer);
                await StartOngoingIndexingJob(secondPassIndexer, ongoingIndexer);
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

            _secondPassHistoryIndexingJobsManager.Stop();
            _ongoingIndexingJobsManager.Stop();

            foreach (var job in _firstPassIndexingJobs)
            {
                job.Wait();
            }

            _secondPassHistoryIndexingJobsManager.Wait();
            _ongoingIndexingJobsManager.Wait();

            _logger.LogInformation("Indexing has been stopped.");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            foreach (var job in _firstPassIndexingJobs)
            {
                job.Dispose();
            }
            
            _secondPassHistoryIndexingJobsManager.Dispose();
            _ongoingIndexingJobsManager.Dispose();
        }

        private async Task<IReadOnlyCollection<FirstPassHistoryIndexer>> ProvisionFirstPassHistoryIndexers(string blockchainId,
            BlockchainIndexingConfig blockchainConfig,
            BlockchainMetamodel blockchainMetamodel)
        {
            var indexerBlocksCount = blockchainConfig.LastHistoricalBlockNumber /
                                     blockchainConfig.FirstPassHistoryIndexersCount;
            var indexerFactoryTasks = Enumerable
                .Range(0, blockchainConfig.FirstPassHistoryIndexersCount)
                .Select(async i =>
                {
                    var startBlock = blockchainMetamodel.Protocol.StartBlockNumber + i * indexerBlocksCount;
                    var isLastIndexer = i != blockchainConfig.FirstPassHistoryIndexersCount - 1;
                    var stopBlock = isLastIndexer
                        ? startBlock + indexerBlocksCount
                        : blockchainConfig.LastHistoricalBlockNumber;
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
                        _logger.LogInformation("First-pass history indexer has been found {@context}", new
                        {
                            BlockchainId = indexer.BlockchainId,
                            StartBlock = indexer.StartBlock,
                            StopBlock = stopBlock,
                            NextBlock = indexer.NextBlock
                        });
                    }

                    return indexer;
                })
                .ToArray();

            await Task.WhenAll(indexerFactoryTasks);

            var indexers = indexerFactoryTasks
                .Select(x => x.Result)
                .Where(x => x != null)
                .ToArray();

            return indexers;
        }
        
        private async Task<SecondPassHistoryIndexer> ProvisionSecondPassHistoryIndexer(string blockchainId,
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
                _logger.LogInformation("Second-pass history indexer has been found {@context}", new
                {
                    BlockchainId = blockchainId,
                    StartBlock = blockchainMetamodel.Protocol.StartBlockNumber,
                    StopBlock = indexer.StopBlock,
                    NextBlock = indexer.NextBlock
                });
            }

            return indexer;
        }

        private async Task<OngoingIndexer> ProvisionOngoingIndexer(string blockchainId,
            BlockchainIndexingConfig blockchainConfig,
            BlockchainMetamodel blockchainMetamodel)
        {
            var indexer = await _ongoingIndexersRepository.GetOrDefault(blockchainId);

            if (indexer == null)
            {
                var startBlock = blockchainConfig.LastHistoricalBlockNumber;
                var sequence = blockchainConfig.LastHistoricalBlockNumber -
                               blockchainMetamodel.Protocol.StartBlockNumber;

                _logger.LogInformation("Ongoing indexer is being created {@context}", new
                {
                    BlockchainId = blockchainId,
                    StartBlock = startBlock,
                    StartSeqence = sequence
                });

                indexer = OngoingIndexer.Create(
                    blockchainId, 
                    startBlock: blockchainConfig.LastHistoricalBlockNumber,
                    startSequence: blockchainConfig.LastHistoricalBlockNumber - blockchainMetamodel.Protocol.StartBlockNumber);

                await _ongoingIndexersRepository.Add(indexer);
            }
            else
            {
                _logger.LogInformation("Ongoing indexer has been found {@context}", new
                {
                    BlockchainId = blockchainId,
                    NextBlock = indexer.NextBlock,
                    Sequence = indexer.Sequence
                });
            }

            return indexer;
        }

        private async Task StartFirstPassHistoryIndexingJobs(
            IReadOnlyCollection<FirstPassHistoryIndexer> indexers,
            IBlocksReader blocksReader)
        {
            foreach (var indexer in indexers)
            {
                if (indexer.IsCompleted)
                {
                    _logger.LogInformation("First-pass history indexer is already completed {@context}", new
                    {
                        BlockchainId = indexer.BlockchainId,
                        StartBlock = indexer.StartBlock,
                        StopBlock = indexer.StopBlock,
                        NextBlock = indexer.NextBlock
                    });
                }
                else
                {
                    var job = new FirstPassHistoryIndexingJob(
                        _loggerFactory.CreateLogger<FirstPassHistoryIndexingJob>(),
                        _loggerFactory,
                        indexer.Id,
                        indexer.StopBlock,
                        _firstPassHistoryIndexersRepository,
                        blocksReader,
                        _blocksRepository,
                        _inMemoryBus,
                        _secondPassHistoryIndexingJobsManager,
                        _appInsight);

                    await job.Start();
                    
                    _firstPassIndexingJobs.Add(job);
                }
            }
        }

        private async Task StartSecondPassHistoryIndexingJob(IReadOnlyCollection<FirstPassHistoryIndexer> firstPassIndexers, 
            SecondPassHistoryIndexer secondPassIndexer)
        {
            if (firstPassIndexers.All(x => x.IsCompleted))
            {
                _logger.LogInformation("All first-pass history indexers are already completed, starting second-pass history indexer {@context}", new
                {
                    BlockchainId = secondPassIndexer.BlockchainId
                });

                if (secondPassIndexer.IsCompleted)
                {
                    _logger.LogInformation("Second-pass history indexer is already completed {@context}", new
                    {
                        BlockchainId = secondPassIndexer.BlockchainId,
                        StopBlock = secondPassIndexer.StopBlock
                    });
                }
                else
                {
                    await _secondPassHistoryIndexingJobsManager.EnsureStarted(secondPassIndexer.BlockchainId);
                }
            }
        }

        private async Task StartOngoingIndexingJob(SecondPassHistoryIndexer secondPassIndexer,
            OngoingIndexer ongoingIndexer)
        {
            if (secondPassIndexer.IsCompleted)
            {
                _logger.LogInformation("Second-pass history indexer is already completed, starting ongoing indexer {@context}", new
                {
                    BlockchainId = secondPassIndexer.BlockchainId
                });

                await _ongoingIndexingJobsManager.EnsureStarted(ongoingIndexer.BlockchainId);
            }
        }
    }
}
