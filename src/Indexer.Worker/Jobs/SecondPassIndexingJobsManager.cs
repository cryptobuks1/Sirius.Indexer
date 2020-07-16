using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class SecondPassIndexingJobsManager : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppConfig _appConfig;
        private readonly ISecondPassIndexersRepository _indexersRepository;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly SemaphoreSlim _lock;
        private readonly ConcurrentDictionary<string, SecondPassIndexingJob> _jobs;
        

        public SecondPassIndexingJobsManager(ILoggerFactory loggerFactory, 
            AppConfig appConfig,
            ISecondPassIndexersRepository indexersRepository,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            OngoingIndexingJobsManager ongoingIndexingJobsManager)
        {
            _loggerFactory = loggerFactory;
            _appConfig = appConfig;
            _indexersRepository = indexersRepository;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            

            _lock = new SemaphoreSlim(1, 1);
            _jobs = new ConcurrentDictionary<string, SecondPassIndexingJob>();
        }

        public async Task EnsureStarted(string blockchainId)
        {
            await _lock.WaitAsync();

            try
            {
                if (!_jobs.ContainsKey(blockchainId))
                {
                    var blockchainConfig = _appConfig.Blockchains[blockchainId];

                    var job = new SecondPassIndexingJob(
                        _loggerFactory.CreateLogger<SecondPassIndexingJob>(),
                        _loggerFactory,
                        blockchainId,
                        blockchainConfig.Indexing.LastHistoricalBlockNumber,
                        _indexersRepository,
                        _blockchainDbUnitOfWorkFactory,
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
