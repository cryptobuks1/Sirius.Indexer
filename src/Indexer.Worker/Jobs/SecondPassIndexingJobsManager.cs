using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.UnspentCoins;
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
        private readonly IInputCoinsRepository _inputCoinsRepository;
        private readonly IUnspentCoinsRepository _unspentCoinsRepository;
        private readonly ISpentCoinsRepository _spentCoinsRepository;
        private readonly IBalanceUpdatesRepository _balanceUpdatesRepository;
        private readonly IFeesRepository _feesRepository;

        public SecondPassIndexingJobsManager(ILoggerFactory loggerFactory, 
            AppConfig appConfig,
            ISecondPassIndexersRepository indexersRepository,
            IBlockHeadersRepository blockHeadersRepository,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IAppInsight appInsight,
            IInputCoinsRepository inputCoinsRepository,
            IUnspentCoinsRepository unspentCoinsRepository,
            ISpentCoinsRepository spentCoinsRepository,
            IBalanceUpdatesRepository balanceUpdatesRepository,
            IFeesRepository feesRepository)
        {
            _loggerFactory = loggerFactory;
            _appConfig = appConfig;
            _indexersRepository = indexersRepository;
            _blockHeadersRepository = blockHeadersRepository;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _appInsight = appInsight;
            _inputCoinsRepository = inputCoinsRepository;
            _unspentCoinsRepository = unspentCoinsRepository;
            _spentCoinsRepository = spentCoinsRepository;
            _balanceUpdatesRepository = balanceUpdatesRepository;
            _feesRepository = feesRepository;

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
                        _inputCoinsRepository,
                        _unspentCoinsRepository,
                        _spentCoinsRepository,
                        _balanceUpdatesRepository,
                        _feesRepository);

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
