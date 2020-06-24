using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Indexing.Common.CoinBlocks;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.Telemetry;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class SecondPassIndexingJobsManager : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppConfig _appConfig;
        private readonly ISecondPassIndexersRepository _indexersRepository;
        private readonly IBlockHeadersRepository _blockHeadersRepository;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IAppInsight _appInsight;
        private readonly SemaphoreSlim _lock;
        private readonly ConcurrentDictionary<string, SecondPassIndexingJob> _jobs;
        private readonly CoinsSecondaryBlockProcessor _secondaryBlockProcessor;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _jobStartBlockers;

        public SecondPassIndexingJobsManager(ILoggerFactory loggerFactory, 
            AppConfig appConfig,
            ISecondPassIndexersRepository indexersRepository,
            IBlockHeadersRepository blockHeadersRepository,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IAppInsight appInsight,
            CoinsSecondaryBlockProcessor secondaryBlockProcessor)
        {
            _loggerFactory = loggerFactory;
            _appConfig = appConfig;
            _indexersRepository = indexersRepository;
            _blockHeadersRepository = blockHeadersRepository;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _appInsight = appInsight;
            _secondaryBlockProcessor = secondaryBlockProcessor;

            _lock = new SemaphoreSlim(1, 1);
            _jobs = new ConcurrentDictionary<string, SecondPassIndexingJob>();
            _jobStartBlockers = new ConcurrentDictionary<string, SemaphoreSlim>();
        }

        public async Task EnsureStarted(string blockchainId)
        {
            while (_jobStartBlockers.ContainsKey(blockchainId))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            await _lock.WaitAsync();

            try
            {
                if (!_jobs.ContainsKey(blockchainId))
                {
                    var blockchainConfig = _appConfig.Indexing.Blockchains[blockchainId];

                    var job = new SecondPassIndexingJob(
                        _loggerFactory.CreateLogger<SecondPassIndexingJob>(),
                        _loggerFactory,
                        blockchainId,
                        blockchainConfig.LastHistoricalBlockNumber,
                        _indexersRepository,
                        _blockHeadersRepository,
                        _ongoingIndexingJobsManager,
                        _appInsight,
                        _secondaryBlockProcessor);

                    _jobs.TryAdd(blockchainId, job);

                    await job.Start();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public bool IsStarted(string blockchainId)
        {
            return _jobs.ContainsKey(blockchainId);
        }

        public void BlockStart(string blockchainId)
        {
            _jobStartBlockers.TryAdd(blockchainId, default);
        }

        public void AllowStart(string blockchainId)
        {
            _jobStartBlockers.TryRemove(blockchainId, out _);
        }

        public void Stop()
        {
            foreach (var job in _jobs.Values)
            {
                job.Stop();
            }
        }

        public async Task Wait()
        {
            foreach (var job in _jobs.Values)
            {
                await job.Wait();
            }
        }
        
        public void Dispose()
        {
            foreach (var job in _jobs.Values)
            {
                job.Dispose();
            }

            _lock.Dispose();
        }
    }
}
