using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.FirstPass;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Domain.Indexing.SecondPass;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Messaging.InMemoryBus;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.Persistence.Entities.FirstPassIndexers;
using Indexer.Common.Persistence.Entities.OngoingIndexers;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
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
        private readonly IBlockchainSchemaBuilder _blockchainSchemaBuilder;
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly IFirstPassIndexersRepository _firstPassIndexersRepository;
        private readonly ISecondPassIndexersRepository _secondPassIndexersRepository;
        private readonly IOngoingIndexersRepository _ongoingIndexersRepository;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly UnspentCoinsFactory _unspentCoinsFactory;
        private readonly IInMemoryBus _inMemoryBus;
        private readonly SecondPassIndexingJobsManager _secondPassIndexingJobsManager;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IBlockReadersProvider _blockReadersProvider;
        private readonly IAppInsight _appInsight;
        private readonly List<FirstPassIndexingJob> _firstPassIndexingJobs;
        
        public IndexingHost(ILogger<IndexingHost> logger,
            ILoggerFactory loggerFactory,
            AppConfig config,
            IBlockchainSchemaBuilder blockchainSchemaBuilder,
            IBlockchainsRepository blockchainsRepository,
            IFirstPassIndexersRepository firstPassIndexersRepository,
            ISecondPassIndexersRepository secondPassIndexersRepository,
            IOngoingIndexersRepository ongoingIndexersRepository,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            UnspentCoinsFactory unspentCoinsFactory,
            IInMemoryBus inMemoryBus,
            SecondPassIndexingJobsManager secondPassIndexingJobsManager,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IBlockReadersProvider blockReadersProvider,
            IAppInsight appInsight)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _config = config;
            _blockchainSchemaBuilder = blockchainSchemaBuilder;
            _blockchainsRepository = blockchainsRepository;
            _firstPassIndexersRepository = firstPassIndexersRepository;
            _secondPassIndexersRepository = secondPassIndexersRepository;
            _ongoingIndexersRepository = ongoingIndexersRepository;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _unspentCoinsFactory = unspentCoinsFactory;
            _inMemoryBus = inMemoryBus;
            _secondPassIndexingJobsManager = secondPassIndexingJobsManager;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _blockReadersProvider = blockReadersProvider;
            _appInsight = appInsight;

            _firstPassIndexingJobs = new List<FirstPassIndexingJob>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Indexing is being started...");

            foreach (var (blockchainId, blockchainConfig) in _config?.Blockchains ?? new Dictionary<string, BlockchainConfig>())
            {
                _logger.LogInformation(@"Blockchain indexing is being provisioned {@context}...",
                    new
                    {
                        BlockchainId = blockchainId,
                        BlockchainConfig = blockchainConfig
                    });

                var blockchainMetamodel = await _blockchainsRepository.GetOrDefaultAsync(blockchainId);

                if (blockchainMetamodel == null)
                {
                    _logger.LogWarning(@"Blockchain metamodel not found. Indexing for this blockchain couldn't be started {@context}",
                        new
                        {
                            BlockchainId = blockchainId,
                            BlockchainConfig = blockchainConfig
                        });

                    continue;
                }

                var blocksReader = await _blockReadersProvider.Get(blockchainId);

                await ProvisionDbSchema(blockchainMetamodel);
                var firstPassIndexers = await ProvisionFirstPassIndexers(blockchainId, blockchainConfig.Indexing, blockchainMetamodel);
                var secondPassIndexer = await ProvisionSecondPassIndexer(blockchainId, blockchainConfig.Indexing, blockchainMetamodel);
                var ongoingIndexer = await ProvisionOngoingIndexer(blockchainId, blockchainConfig.Indexing, blockchainMetamodel);
                
                await StartFirstPassIndexingJobs(firstPassIndexers, blocksReader);
                await StartSecondPassIndexingJob(firstPassIndexers, secondPassIndexer);
                await StartOngoingIndexingJob(secondPassIndexer, ongoingIndexer);
            }

            _logger.LogInformation("Indexing has been started.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Indexing is being stopped...");

            foreach (var job in _firstPassIndexingJobs)
            {
                job.Stop();
            }

            _secondPassIndexingJobsManager.Stop();
            _ongoingIndexingJobsManager.Stop();

            foreach (var job in _firstPassIndexingJobs)
            {
                await job.Wait();
            }

            await _secondPassIndexingJobsManager.Wait();
            _ongoingIndexingJobsManager.Wait();

            _logger.LogInformation("Indexing has been stopped.");
        }

        public void Dispose()
        {
            foreach (var job in _firstPassIndexingJobs)
            {
                job.Dispose();
            }
            
            _secondPassIndexingJobsManager.Dispose();
            _ongoingIndexingJobsManager.Dispose();
        }

        private async Task ProvisionDbSchema(BlockchainMetamodel blockchainMetamodel)
        {
            if (await _blockchainSchemaBuilder.ProvisionForIndexing(blockchainMetamodel.Id, blockchainMetamodel.Protocol.DoubleSpendingProtectionType))
            {
                _logger.LogInformation("Cleaning {@blockchainId} indexers up since schema was just created...", blockchainMetamodel.Id);

                await _firstPassIndexersRepository.Remove(blockchainMetamodel.Id);
                await _secondPassIndexersRepository.Remove(blockchainMetamodel.Id);
                await _ongoingIndexersRepository.Remove(blockchainMetamodel.Id);

                _logger.LogInformation("{@blockchainId} indexers cleaned up", blockchainMetamodel.Id);
            }
        }

        private async Task<IReadOnlyCollection<FirstPassIndexer>> ProvisionFirstPassIndexers(string blockchainId,
            BlockchainIndexingConfig blockchainConfig,
            BlockchainMetamodel blockchainMetamodel)
        {
            var indexerFactoryTasks = Enumerable
                .Range(0, blockchainConfig.FirstPassIndexersCount)
                .Select(async i =>
                {
                    var startBlock = blockchainMetamodel.Protocol.StartBlockNumber + i;
                    var stopBlock = blockchainConfig.LastHistoricalBlockNumber;
                    var stepSize = blockchainConfig.FirstPassIndexersCount;
                    var indexerId = new FirstPassIndexerId(blockchainId, startBlock);
                    var indexer = await _firstPassIndexersRepository.GetOrDefault(indexerId);

                    if (indexer == null)
                    {
                        _logger.LogInformation("First-pass indexer is being created {@context}", new
                        {
                            BlockchainId = indexerId.BlockchainId,
                            StartBlock = indexerId.StartBlock,
                            StopBlock = stopBlock,
                            StepSize = stepSize
                        });

                        indexer = FirstPassIndexer.Start(indexerId, stopBlock, stepSize);

                        await _firstPassIndexersRepository.Add(indexer);
                    }
                    else
                    {
                        _logger.LogInformation("First-pass indexer has been found {@context}", new
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
        
        private async Task<SecondPassIndexer> ProvisionSecondPassIndexer(string blockchainId,
            BlockchainIndexingConfig blockchainConfig,
            BlockchainMetamodel blockchainMetamodel)
        {
            var indexer = await _secondPassIndexersRepository.GetOrDefault(blockchainId);

            if (indexer == null)
            {
                _logger.LogInformation("Second-pass indexer is being created {@context}", new
                {
                    BlockchainId = blockchainId,
                    StartBlock = blockchainMetamodel.Protocol.StartBlockNumber,
                    StopBlock = blockchainConfig.LastHistoricalBlockNumber
                });

                indexer = SecondPassIndexer.Start(
                    blockchainId, 
                    startBlock: blockchainMetamodel.Protocol.StartBlockNumber,
                    stopBlock: blockchainConfig.LastHistoricalBlockNumber);

                await _secondPassIndexersRepository.Add(indexer);
            }
            else
            {
                _logger.LogInformation("Second-pass indexer has been found {@context}", new
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
                    Seqence = sequence
                });

                indexer = OngoingIndexer.Start(
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

        private async Task StartFirstPassIndexingJobs(
            IReadOnlyCollection<FirstPassIndexer> indexers,
            IBlocksReader blocksReader)
        {
            foreach (var indexer in indexers)
            {
                if (indexer.IsCompleted)
                {
                    _logger.LogInformation("First-pass indexer is already completed {@context}", new
                    {
                        BlockchainId = indexer.BlockchainId,
                        StartBlock = indexer.StartBlock,
                        StopBlock = indexer.StopBlock,
                        NextBlock = indexer.NextBlock
                    });
                }
                else
                {
                    var job = new FirstPassIndexingJob(
                        _loggerFactory.CreateLogger<FirstPassIndexingJob>(),
                        _loggerFactory,
                        indexer.Id,
                        indexer.StopBlock,
                        _firstPassIndexersRepository,
                        blocksReader,
                        _inMemoryBus,
                        _blockchainDbUnitOfWorkFactory,
                        _unspentCoinsFactory,
                        _secondPassIndexingJobsManager, 
                        _appInsight);

                    await job.Start();
                    
                    _firstPassIndexingJobs.Add(job);
                }
            }
        }

        private async Task StartSecondPassIndexingJob(IReadOnlyCollection<FirstPassIndexer> firstPassIndexers, 
            SecondPassIndexer secondPassIndexer)
        {
            if (firstPassIndexers.All(x => x.IsCompleted))
            {
                _logger.LogInformation("All first-pass indexers are already completed, starting second-pass indexer {@context}", new
                {
                    BlockchainId = secondPassIndexer.BlockchainId
                });

                if (secondPassIndexer.IsCompleted)
                {
                    _logger.LogInformation("Second-pass indexer is already completed {@context}", new
                    {
                        BlockchainId = secondPassIndexer.BlockchainId,
                        StopBlock = secondPassIndexer.StopBlock
                    });
                }
                else
                {
                    await _secondPassIndexingJobsManager.EnsureStarted(secondPassIndexer.BlockchainId);
                }
            }
        }

        private async Task StartOngoingIndexingJob(SecondPassIndexer secondPassIndexer,
            OngoingIndexer ongoingIndexer)
        {
            if (secondPassIndexer.IsCompleted)
            {
                _logger.LogInformation("Second-pass indexer is already completed, starting ongoing indexer {@context}", new
                {
                    BlockchainId = secondPassIndexer.BlockchainId
                });

                await _ongoingIndexingJobsManager.EnsureStarted(ongoingIndexer.BlockchainId);
            }
        }
    }
}
