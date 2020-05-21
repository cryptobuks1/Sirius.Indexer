using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Indexing;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class OngoingIndexingJobsManager : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppConfig _appConfig;
        private readonly IOngoingIndexersRepository _indexersRepository;
        private readonly IBlockReadersProvider _blockReadersProvider;
        private readonly BlocksProcessor _blocksProcessor;
        private readonly IPublishEndpoint _publisher;
        private readonly SemaphoreSlim _lock;
        private readonly ConcurrentDictionary<string, OngoingIndexingJob> _jobs;

        public OngoingIndexingJobsManager(ILoggerFactory loggerFactory, 
            AppConfig appConfig,
            IOngoingIndexersRepository indexersRepository,
            IBlockReadersProvider blockReadersProvider,
            BlocksProcessor blocksProcessor,
            IPublishEndpoint publisher)
        {
            _loggerFactory = loggerFactory;
            _appConfig = appConfig;
            _indexersRepository = indexersRepository;
            _blockReadersProvider = blockReadersProvider;
            _blocksProcessor = blocksProcessor;
            _publisher = publisher;

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
                    var blockchainConfig = _appConfig.Indexing.Blockchains[blockchainId];
                    var blocksReader = await _blockReadersProvider.Get(blockchainId);

                    var job = new OngoingIndexingJob(
                        _loggerFactory.CreateLogger<OngoingIndexingJob>(),
                        _loggerFactory,
                        blockchainId,
                        blockchainConfig.DelayOnBlockNotFound,
                        _indexersRepository,
                        blocksReader,
                        _blocksProcessor,
                        _publisher);

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
