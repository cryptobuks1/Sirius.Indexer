using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Indexing.FirstPass;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Domain.Indexing.SecondPass;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.Persistence.Entities.FirstPassIndexers;
using Indexer.Common.Persistence.Entities.OngoingIndexers;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.ReadModel.Blockchains;
using Indexer.Common.Telemetry;
using Indexer.Worker.Jobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Primitives;

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
        private readonly SecondPassIndexingJobsManager _secondPassIndexingJobsManager;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IAppInsight _appInsight;
        private readonly List<FirstPassIndexingJob> _firstPassIndexingJobs;
        private readonly FirstPassIndexingStrategyFactory _firstPassIndexingStrategyFactory;

        public IndexingHost(ILogger<IndexingHost> logger,
            ILoggerFactory loggerFactory,
            AppConfig config,
            IBlockchainSchemaBuilder blockchainSchemaBuilder,
            IBlockchainsRepository blockchainsRepository,
            IFirstPassIndexersRepository firstPassIndexersRepository,
            ISecondPassIndexersRepository secondPassIndexersRepository,
            IOngoingIndexersRepository ongoingIndexersRepository,
            SecondPassIndexingJobsManager secondPassIndexingJobsManager,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IAppInsight appInsight,
            FirstPassIndexingStrategyFactory firstPassIndexingStrategyFactory)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _config = config;
            _blockchainSchemaBuilder = blockchainSchemaBuilder;
            _blockchainsRepository = blockchainsRepository;
            _firstPassIndexersRepository = firstPassIndexersRepository;
            _secondPassIndexersRepository = secondPassIndexersRepository;
            _ongoingIndexersRepository = ongoingIndexersRepository;
            _secondPassIndexingJobsManager = secondPassIndexingJobsManager;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _appInsight = appInsight;
            _firstPassIndexingStrategyFactory = firstPassIndexingStrategyFactory;

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
                        BlockchainConfig = blockchainConfig.Indexing
                    });

                var blockchainMetamodel = await _blockchainsRepository.GetOrDefaultAsync(blockchainId);

                if (blockchainMetamodel == null)
                {
                    _logger.LogWarning(@"Blockchain metamodel not found. Indexing for this blockchain couldn't be started {@context}",
                        new
                        {
                            BlockchainId = blockchainId,
                            BlockchainConfig = blockchainConfig.Indexing
                        });

                    continue;
                }

                await ProvisionDbSchema(blockchainMetamodel);
                var firstPassIndexers = await ProvisionFirstPassIndexers(blockchainId, blockchainConfig.Indexing, blockchainMetamodel);
                var secondPassIndexer = await ProvisionSecondPassIndexerOrDefault(blockchainId, blockchainConfig.Indexing, blockchainMetamodel);
                var ongoingIndexer = await ProvisionOngoingIndexer(blockchainId, blockchainConfig.Indexing, blockchainMetamodel);
                
                await StartFirstPassIndexingJobs(firstPassIndexers, blockchainMetamodel);
                await StartSecondPassIndexingJob(secondPassIndexer);
                await StartOngoingIndexingJob(
                    firstPassIndexers,
                    secondPassIndexer,
                    ongoingIndexer,
                    blockchainMetamodel);
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
        
        private async Task<SecondPassIndexer> ProvisionSecondPassIndexerOrDefault(string blockchainId,
            BlockchainIndexingConfig blockchainConfig,
            BlockchainMetamodel blockchainMetamodel)
        {
            if (blockchainMetamodel.Protocol.DoubleSpendingProtectionType != DoubleSpendingProtectionType.Coins)
            {
                _logger.LogInformation($"Second-pass indexer creation is skipped since blockchain double spending protection type is not {nameof(DoubleSpendingProtectionType.Coins)} {{@context}}", new
                {
                    BlockchainId = blockchainId,
                    DoubleSpendingProtectionType = blockchainMetamodel.Protocol.DoubleSpendingProtectionType
                });

                return null;
            }

            if (blockchainConfig.FirstPassIndexersCount == 0)
            {
                _logger.LogInformation("Second-pass indexer creation is skipped since there are no first-pass indexers configured {@context}", new
                {
                    BlockchainId = blockchainId
                });

                return null;
            }

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

        private async Task StartFirstPassIndexingJobs(IReadOnlyCollection<FirstPassIndexer> indexers, BlockchainMetamodel blockchainMetamodel)
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
                        indexer.Id,
                        indexer.StopBlock,
                        blockchainMetamodel,
                        _firstPassIndexersRepository,
                        _appInsight,
                        _firstPassIndexingStrategyFactory,
                        _ongoingIndexingJobsManager);

                    await job.Start();
                    
                    _firstPassIndexingJobs.Add(job);
                }
            }
        }

        private async Task StartSecondPassIndexingJob(SecondPassIndexer secondPassIndexer)
        {
            if (secondPassIndexer == null)
            {
                return;
            }

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

        private async Task StartOngoingIndexingJob(
            IReadOnlyCollection<FirstPassIndexer> firstPassIndexers,
            SecondPassIndexer secondPassIndexer,
            OngoingIndexer ongoingIndexer,
            BlockchainMetamodel blockchainMetamodel)
        {
            var start = false;

            switch (blockchainMetamodel.Protocol.DoubleSpendingProtectionType)
            {
                case DoubleSpendingProtectionType.Coins:
                    if (secondPassIndexer == null)
                    {
                        _logger.LogInformation("There are no second-pass indexer found for the coins blockchain, starting ongoing indexer immediately {@context}",
                            new {BlockchainId = ongoingIndexer.BlockchainId});

                        start = true;
                    }
                    else if (secondPassIndexer.IsCompleted)
                    {
                        _logger.LogInformation("Second-pass indexer is already completed, starting ongoing indexer {@context}",
                            new {BlockchainId = ongoingIndexer.BlockchainId});

                        start = true;
                    }
                    break;

                case DoubleSpendingProtectionType.Nonce:
                    if (!firstPassIndexers.Any())
                    {
                        _logger.LogInformation("There are no first-pass indexers configured, starting ongoing indexer immediately {@context}",
                            new {BlockchainId = ongoingIndexer.BlockchainId});

                        start = true;
                    } 
                    else if (firstPassIndexers.All(x => x.IsCompleted))
                    {
                        _logger.LogInformation("All first-pass indexers are already completed, starting ongoing indexer immediately {@context}",
                            new {BlockchainId = ongoingIndexer.BlockchainId});

                        start = true;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockchainMetamodel.Protocol.DoubleSpendingProtectionType), blockchainMetamodel.Protocol.DoubleSpendingProtectionType, "");
            }

            if (start)
            {
                await _ongoingIndexingJobsManager.EnsureStarted(ongoingIndexer.BlockchainId);
            }
        }
    }
}
