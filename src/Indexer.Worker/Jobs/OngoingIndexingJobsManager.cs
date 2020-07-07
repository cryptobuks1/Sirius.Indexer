using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Blockchains;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling;
using Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.Persistence.Entities.OngoingIndexers;
using Indexer.Common.Telemetry;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class OngoingIndexingJobsManager : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppConfig _appConfig;
        private readonly IBlockchainSchemaBuilder _blockchainSchemaBuilder;
        private readonly IOngoingIndexersRepository _indexersRepository;
        private readonly IBlockchainMetamodelProvider _blockchainMetamodelProvider;
        private readonly ChainWalker _chainWalker;
        private readonly IAppInsight _appInsight;
        private readonly SemaphoreSlim _lock;
        private readonly ConcurrentDictionary<string, OngoingIndexingJob> _jobs;
        private readonly OngoingIndexingStrategyFactory _ongoingIndexingStrategyFactory;
        private readonly BlockCancelerFactory _blockCancelerFactory;

        public OngoingIndexingJobsManager(ILoggerFactory loggerFactory, 
            AppConfig appConfig,
            IBlockchainSchemaBuilder blockchainSchemaBuilder,
            IOngoingIndexersRepository indexersRepository,
            IBlockchainMetamodelProvider blockchainMetamodelProvider,
            ChainWalker chainWalker,
            IAppInsight appInsight,
            OngoingIndexingStrategyFactory ongoingIndexingStrategyFactory,
            BlockCancelerFactory blockCancelerFactory)
        {
            _loggerFactory = loggerFactory;
            _appConfig = appConfig;
            _blockchainSchemaBuilder = blockchainSchemaBuilder;
            _indexersRepository = indexersRepository;
            _blockchainMetamodelProvider = blockchainMetamodelProvider;
            _chainWalker = chainWalker;
            _appInsight = appInsight;
            _ongoingIndexingStrategyFactory = ongoingIndexingStrategyFactory;
            _blockCancelerFactory = blockCancelerFactory;

            _lock = new SemaphoreSlim(1, 1);
            _jobs = new ConcurrentDictionary<string, OngoingIndexingJob>();
        }

        public async Task EnsureStarted(string blockchainId)
        {
            await _lock.WaitAsync();

            try
            {
                if (!_jobs.ContainsKey(blockchainId))
                {
                    var blockchainConfig = _appConfig.Blockchains[blockchainId];
                    var blockchainMetadata = await _blockchainMetamodelProvider.Get(blockchainId);

                    var job = new OngoingIndexingJob(
                        _loggerFactory.CreateLogger<OngoingIndexingJob>(),
                        blockchainId,
                        blockchainMetadata.Protocol.DoubleSpendingProtectionType,
                        blockchainConfig.Indexing.DelayOnBlockNotFound,
                        _blockchainSchemaBuilder,
                        _indexersRepository,
                        _chainWalker,
                        _appInsight,
                        _ongoingIndexingStrategyFactory,
                        _blockCancelerFactory);

                    _jobs.TryAdd(blockchainId, job);

                    await job.Start();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            foreach (var job in _jobs.Values)
            {
                job.Dispose();
            }
        }

        public void Stop()
        {
            foreach (var job in _jobs.Values)
            {
                job.Stop();
            }
        }

        public void Wait()
        {
            foreach (var job in _jobs.Values)
            {
                job.Wait();
            }
        }
    }
}
