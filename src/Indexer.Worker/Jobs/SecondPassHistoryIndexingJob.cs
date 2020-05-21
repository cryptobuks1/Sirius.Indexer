using System;
using System.Threading.Tasks;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class SecondPassHistoryIndexingJob : IDisposable
    {
        private readonly ILogger<SecondPassHistoryIndexingJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly string _blockchainId;
        private readonly long _stopBlock;
        private readonly ISecondPassHistoryIndexersRepository _indexersRepository;
        private readonly IBlocksRepository _blocksRepository;
        private readonly IPublishEndpoint _publisher;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly BackgroundJob _job;
        private SecondPassHistoryIndexer _indexer;

        public SecondPassHistoryIndexingJob(ILogger<SecondPassHistoryIndexingJob> logger,
            ILoggerFactory loggerFactory,
            string blockchainId,
            long stopBlock,
            ISecondPassHistoryIndexersRepository indexersRepository,
            IBlocksRepository blocksRepository,
            IPublishEndpoint publisher,
            OngoingIndexingJobsManager ongoingIndexingJobsManager)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _blockchainId = blockchainId;
            _stopBlock = stopBlock;
            _indexersRepository = indexersRepository;
            _blocksRepository = blocksRepository;
            _publisher = publisher;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;

            _job = new BackgroundJob(
                loggerFactory.CreateLogger<SecondPassHistoryIndexingJob>(),
                "Second-pass history indexing",
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
            // TODO: Move max blocks count to config
            var indexingResult = await _indexer.IndexAvailableBlocks(
                _loggerFactory.CreateLogger<SecondPassHistoryIndexer>(), 
                maxBlocksCount: 100,
                _blocksRepository,
                _publisher);

            if (indexingResult == SecondPassHistoryIndexingResult.IndexingCompleted)
            {
                _logger.LogInformation("Second-pass history indexing job is completed {@context}", new
                {
                    BlockchainId = _blockchainId,
                    StopBlock = _stopBlock
                });

                await _ongoingIndexingJobsManager.EnsureStarted(_blockchainId);

                Stop();
            }

            // TODO: Update indexer Version or re-read it from DB

            await _indexersRepository.Update(_indexer);
        }
    }
}
