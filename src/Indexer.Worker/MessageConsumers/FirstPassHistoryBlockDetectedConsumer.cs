using System;
using System.Threading.Tasks;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Monitoring;
using Indexer.Worker.Jobs;
using Indexer.Worker.Limiters;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.MessageConsumers
{
    internal class FirstPassHistoryBlockDetectedConsumer : IConsumer<FirstPassHistoryBlockDetected>
    {
        private static readonly RateLimiter RateLimiter = new RateLimiter(1, TimeSpan.FromSeconds(1));
        private static readonly ConcurrencyLimiter ConcurrencyLimiter = new ConcurrencyLimiter(1);

        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlocksRepository _blocksRepository;
        private readonly ISecondPassHistoryIndexersRepository _secondPassHistoryIndexersRepository;
        private readonly IPublishEndpoint _persistentPublisher;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;
        private readonly IAppInsight _appInsight;

        public FirstPassHistoryBlockDetectedConsumer(ILoggerFactory loggerFactory,
            IBlocksRepository blocksRepository,
            ISecondPassHistoryIndexersRepository secondPassHistoryIndexersRepository,
            IPublishEndpoint persistentPublisher,
            OngoingIndexingJobsManager ongoingIndexingJobsManager,
            IAppInsight appInsight)
        {
            _loggerFactory = loggerFactory;
            _blocksRepository = blocksRepository;
            _secondPassHistoryIndexersRepository = secondPassHistoryIndexersRepository;
            _persistentPublisher = persistentPublisher;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
            _appInsight = appInsight;
        }

        public async Task Consume(ConsumeContext<FirstPassHistoryBlockDetected> context)
        {
            var evt = context.Message;

            if (!await RateLimiter.Wait($"{nameof(FirstPassHistoryBlockDetected)}-{evt.BlockchainId}"))
            {
                // Just skips rate-limited event
                return;
            }

            using var concurrencyLimiter = await ConcurrencyLimiter.Enter($"{nameof(FirstPassHistoryBlockDetected)}-{evt.BlockchainId}");

            var secondPassIndexer = await _secondPassHistoryIndexersRepository.Get(evt.BlockchainId);

            var secondPassIndexingResult = await secondPassIndexer.IndexAvailableBlocks(
                _loggerFactory.CreateLogger<SecondPassHistoryIndexer>(),
                // TODO: To config
                100,
                _blocksRepository,
                _persistentPublisher,
                _appInsight);

            await _secondPassHistoryIndexersRepository.Update(secondPassIndexer);

            if (secondPassIndexingResult == SecondPassHistoryIndexingResult.IndexingCompleted)
            {
                await _ongoingIndexingJobsManager.EnsureStarted(evt.BlockchainId);
            }
        }
    }
}
