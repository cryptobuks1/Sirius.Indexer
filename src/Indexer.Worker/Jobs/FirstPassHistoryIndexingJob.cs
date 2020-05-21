using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Messaging.InMemoryBus;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.Jobs
{
    internal sealed class FirstPassHistoryIndexingJob : IDisposable
    {
        private readonly ILogger<FirstPassHistoryIndexingJob> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly FirstPassHistoryIndexerId _indexerId;
        private readonly long _stopBlock;
        private readonly IFirstPassHistoryIndexersRepository _indexersRepository;
        private readonly IBlocksReader _blocksReader;
        private readonly IBlocksRepository _blocksRepository;
        private readonly IInMemoryBus _inMemoryBus;
        private readonly SecondPassHistoryIndexingJobsManager _secondPassHistoryIndexingJobsManager;
        private readonly BackgroundJob _job;
        private FirstPassHistoryIndexer _indexer;

        public FirstPassHistoryIndexingJob(ILogger<FirstPassHistoryIndexingJob> logger,
            ILoggerFactory loggerFactory,
            FirstPassHistoryIndexerId indexerId,
            long stopBlock,
            IFirstPassHistoryIndexersRepository indexersRepository,
            IBlocksReader blocksReader,
            IBlocksRepository blocksRepository,
            IInMemoryBus inMemoryBus,
            SecondPassHistoryIndexingJobsManager secondPassHistoryIndexingJobsManager)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _indexerId = indexerId;
            _stopBlock = stopBlock;
            _indexersRepository = indexersRepository;
            _blocksReader = blocksReader;
            _blocksRepository = blocksRepository;
            _inMemoryBus = inMemoryBus;
            _secondPassHistoryIndexingJobsManager = secondPassHistoryIndexingJobsManager;

            _job = new BackgroundJob(
                loggerFactory.CreateLogger<SecondPassHistoryIndexingJob>(),
                "First-pass indexing",
                new
                {
                    BlockchainId = _indexerId.BlockchainId,
                    StartBlock = _indexerId.StartBlock,
                    StopBlock = _stopBlock
                },
                IndexBlocksBatch);
        }

        public async Task Start()
        {
            _indexer = await _indexersRepository.Get(_indexerId);

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
            var batchInitialBlock = _indexer.NextBlock;

            // TODO: Move batch size to the config

            while (!_job.IsCancellationRequested &&
                   _indexer.NextBlock - batchInitialBlock < 100)
            {
                var indexingResult = await _indexer.IndexNextBlock(
                    _loggerFactory.CreateLogger<FirstPassHistoryIndexer>(),
                    _blocksReader,
                    _blocksRepository,
                    _inMemoryBus);

                if (indexingResult == FirstPassHistoryIndexingResult.IndexingCompleted)
                {
                    _logger.LogInformation("First-pass history indexing job is completed {@context}", new
                    {
                        BlockchainId = _indexerId.BlockchainId,
                        StartBlock = _indexerId.StartBlock,
                        StopBlock = _stopBlock
                    });

                    await _indexersRepository.Update(_indexer);

                    await StartSecondPassIndexerJobIfFirstPassDone();

                    Stop();

                    return;
                }
            }

            // Saves the indexer state only in the end of the batch

            // TODO: Update indexer Version or re-read it from DB
            await _indexersRepository.Update(_indexer);
        }

        private async Task StartSecondPassIndexerJobIfFirstPassDone()
        {
            var indexers = await _indexersRepository.GetByBlockchain(_indexerId.BlockchainId);

            if (indexers.All(x => x.IsCompleted))
            {
                await _secondPassHistoryIndexingJobsManager.EnsureStarted(_indexerId.BlockchainId);
            }
        }
    }
}
