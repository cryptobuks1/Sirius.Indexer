using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class SecondPassHistoryIndexingJobsManager : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppConfig _appConfig;
        private readonly ISecondPassHistoryIndexersRepository _indexersRepository;
        private readonly IBlocksRepository _blocksRepository;
        private readonly IPublishEndpoint _publisher;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly SemaphoreSlim _lock;
        private readonly ConcurrentDictionary<string, SecondPassHistoryIndexingJob> _jobs;

        public SecondPassHistoryIndexingJobsManager(ILoggerFactory loggerFactory, 
            AppConfig appConfig,
            ISecondPassHistoryIndexersRepository indexersRepository,
            IBlocksRepository blocksRepository,
            IPublishEndpoint publisher,
            OngoingIndexingJobsManager ongoingIndexingJobsManager)
        {
            _loggerFactory = loggerFactory;
            _appConfig = appConfig;
            _indexersRepository = indexersRepository;
            _blocksRepository = blocksRepository;
            _publisher = publisher;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;

            _lock = new SemaphoreSlim(1, 1);
            _jobs = new ConcurrentDictionary<string, SecondPassHistoryIndexingJob>();
        }

        public async Task EnsureStarted(string blockchainId)
        {
            await _lock.WaitAsync();

            try
            {
                if (!_jobs.ContainsKey(blockchainId))
                {
                    var blockchainConfig = _appConfig.Indexing.Blockchains[blockchainId];

                    var job = new SecondPassHistoryIndexingJob(
                        _loggerFactory.CreateLogger<SecondPassHistoryIndexingJob>(),
                        _loggerFactory,
                        blockchainId,
                        blockchainConfig.LastHistoricalBlockNumber,
                        _indexersRepository,
                        _blocksRepository,
                        _publisher,
                        _ongoingIndexingJobsManager);

                    _jobs.TryAdd(blockchainId, job);

                    await job.Start();
                }
            }
            finally
            {
                _lock.Release();
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
