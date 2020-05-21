using System.Threading.Tasks;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using Indexer.Worker.Jobs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.MessageConsumers
{
    internal class FirstPassHistoryBlockDetectedConsumer : IConsumer<FirstPassHistoryBlockDetected>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlocksRepository _blocksRepository;
        private readonly ISecondPassHistoryIndexersRepository _secondPassHistoryIndexersRepository;
        private readonly IPublishEndpoint _persistentPublisher;
        private readonly OngoingIndexingJobsManager _ongoingIndexingJobsManager;

        public FirstPassHistoryBlockDetectedConsumer(ILoggerFactory loggerFactory,
            IBlocksRepository blocksRepository,
            ISecondPassHistoryIndexersRepository secondPassHistoryIndexersRepository,
            IPublishEndpoint persistentPublisher,
            OngoingIndexingJobsManager ongoingIndexingJobsManager)
        {
            _loggerFactory = loggerFactory;
            _blocksRepository = blocksRepository;
            _secondPassHistoryIndexersRepository = secondPassHistoryIndexersRepository;
            _persistentPublisher = persistentPublisher;
            _ongoingIndexingJobsManager = ongoingIndexingJobsManager;
        }

        public async Task Consume(ConsumeContext<FirstPassHistoryBlockDetected> context)
        {
            var evt = context.Message;
            var secondPassIndexer = await _secondPassHistoryIndexersRepository.Get(evt.BlockchainId);

            var secondPassIndexingResult = await secondPassIndexer.IndexAvailableBlocks(
                _loggerFactory.CreateLogger<SecondPassHistoryIndexer>(),
                // TODO: To config
                100,
                _blocksRepository,
                _persistentPublisher);

            await _secondPassHistoryIndexersRepository.Update(secondPassIndexer);

            if (secondPassIndexingResult == SecondPassHistoryIndexingResult.IndexingCompleted)
            {
                await _ongoingIndexingJobsManager.EnsureStarted(evt.BlockchainId);
            }
        }
    }
}
