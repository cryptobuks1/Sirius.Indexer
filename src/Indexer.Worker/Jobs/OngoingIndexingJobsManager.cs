using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.Persistence.Entities.OngoingIndexers;
using Indexer.Common.Persistence.Entities.TransactionHeaders;
using Indexer.Common.Telemetry;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class OngoingIndexingJobsManager : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly AppConfig _appConfig;
        private readonly IBlockchainSchemaBuilder _blockchainSchemaBuilder;
        private readonly IOngoingIndexersRepository _indexersRepository;
        private readonly ITransactionHeadersRepository _transactionHeadersRepository;
        private readonly IBlockReadersProvider _blockReadersProvider;
        private readonly ChainWalker _chainWalker;
        private readonly IPublishEndpoint _publisher;
        private readonly IAppInsight _appInsight;
        private readonly SemaphoreSlim _lock;
        private readonly ConcurrentDictionary<string, OngoingIndexingJob> _jobs;

        public OngoingIndexingJobsManager(ILoggerFactory loggerFactory, 
            AppConfig appConfig,
            IBlockchainSchemaBuilder blockchainSchemaBuilder,
            IOngoingIndexersRepository indexersRepository,
            ITransactionHeadersRepository transactionHeadersRepository,
            IBlockReadersProvider blockReadersProvider,
            ChainWalker chainWalker,
            IPublishEndpoint publisher,
            IAppInsight appInsight)
        {
            _loggerFactory = loggerFactory;
            _appConfig = appConfig;
            _blockchainSchemaBuilder = blockchainSchemaBuilder;
            _indexersRepository = indexersRepository;
            _transactionHeadersRepository = transactionHeadersRepository;
            _blockReadersProvider = blockReadersProvider;
            _chainWalker = chainWalker;
            _publisher = publisher;
            _appInsight = appInsight;

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
                        _blockchainSchemaBuilder,
                        _indexersRepository,
                        _transactionHeadersRepository,
                        blocksReader,
                        _chainWalker,
                        _publisher,
                        _appInsight);

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
