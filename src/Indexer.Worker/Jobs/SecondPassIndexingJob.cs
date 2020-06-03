using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Telemetry;
using MassTransit;
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
        private readonly IPublishEndpoint _publisher;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IAppInsight _appInsight;
        private readonly BackgroundJob _job;
        private SecondPassIndexer _indexer;

        public SecondPassIndexingJob(ILogger<SecondPassIndexingJob> logger,
            ILoggerFactory loggerFactory,
            string blockchainId,
            long stopBlock,
            ISecondPassIndexersRepository indexersRepository,
            IBlockHeadersRepository blockHeadersRepository,
            IPublishEndpoint publisher,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IAppInsight appInsight)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _blockchainId = blockchainId;
            _stopBlock = stopBlock;
            _indexersRepository = indexersRepository;
            _blockHeadersRepository = blockHeadersRepository;
            _publisher = publisher;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _appInsight = appInsight;

            _job = new BackgroundJob(
                _logger,
                "Second-pass indexing",
                new
                {
                    BlockchainId = _blockchainId,
                    StopBlock = _stopBlock
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
                // TODO: Add some delay in case of an error to reduce workload on the DB
                // TODO: Move max blocks count to config
                var indexingResult = await _indexer.IndexAvailableBlocks(
                    _loggerFactory.CreateLogger<SecondPassIndexer>(),
                    maxBlocksCount:
                    100, // For the bitcoin-test on azure 100 is not enough. About 200 should be ok I guess
                    _blockHeadersRepository,
                    _publisher,
                    _appInsight);

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
                _logger.LogError(ex, "Failed to execute second-pass indexing job");

                _indexer = await _indexersRepository.Get(_blockchainId);
            }
        }
    }
}
