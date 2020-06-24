using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing.Common.CoinBlocks;
using Indexer.Common.Domain.Indexing.SecondPass;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.Telemetry;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class SecondPassIndexingJob : IDisposable
    {
        private readonly ILogger<SecondPassIndexingJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _blockchainId;
        private readonly long _stopBlock;
        private readonly ISecondPassIndexersRepository _indexersRepository;
        private readonly IBlockHeadersRepository _blockHeadersRepository;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IAppInsight _appInsight;
        private readonly BackgroundJob _job;
        private SecondPassIndexer _indexer;
        private readonly CoinsSecondaryBlockProcessor _coinsSecondaryBlockProcessor;

        public SecondPassIndexingJob(ILogger<SecondPassIndexingJob> logger,
            ILoggerFactory loggerFactory,
            string blockchainId,
            long stopBlock,
            ISecondPassIndexersRepository indexersRepository,
            IBlockHeadersRepository blockHeadersRepository,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IAppInsight appInsight,
            CoinsSecondaryBlockProcessor coinsSecondaryBlockProcessor)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _blockchainId = blockchainId;
            _stopBlock = stopBlock;
            _indexersRepository = indexersRepository;
            _blockHeadersRepository = blockHeadersRepository;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _appInsight = appInsight;
            _coinsSecondaryBlockProcessor = coinsSecondaryBlockProcessor;

            _job = new BackgroundJob(
                _logger,
                "Second-pass indexing",
                () => new
                {
                    BlockchainId = _blockchainId,
                    StopBlock = _stopBlock,
                    NextBlock = _indexer?.NextBlock
                },
                IndexBlocksBatch);
        }

        public async Task Start()
        {
            _indexer = await _indexersRepository.Get(_blockchainId);

            _job.Start();
        }

        public void Stop()
        {
            _job.Stop();
        }

        public Task Wait()
        {
            return _job.Wait();
        }

        public void Dispose()
        {
            _job.Dispose();
        }

        private async Task IndexBlocksBatch()
        {
            try
            {
                // TODO: Move max blocks count to config
                var indexingResult = await _indexer.IndexAvailableBlocks(
                    _loggerFactory.CreateLogger<SecondPassIndexer>(),
                    maxBlocksCount: 100,
                    _blockHeadersRepository,
                    _appInsight,
                    _coinsSecondaryBlockProcessor);

                if (indexingResult == SecondPassIndexingResult.IndexingCompleted)
                {
                    _logger.LogInformation("Second-pass indexing job is completed {@context}",
                        new
                        {
                            BlockchainId = _blockchainId,
                            StopBlock = _stopBlock
                        });

                    await _ongoingIndexingJobsManager.EnsureStarted(_blockchainId);

                    Stop();
                }

                _indexer = await _indexersRepository.Update(_indexer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute second-pass indexing job {@context}", new
                {
                    BlockchainId = _blockchainId,
                    StopBlock = _stopBlock,
                    NextBlock = _indexer.NextBlock
                });

                _indexer = await _indexersRepository.Get(_blockchainId);

                await Task.Delay(TimeSpan.FromSeconds(30));
            }
        }
    }
}
