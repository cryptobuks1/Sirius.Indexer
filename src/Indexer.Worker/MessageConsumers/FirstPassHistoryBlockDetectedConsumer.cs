using System.Threading.Tasks;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.MessageConsumers
{
    public class FirstPassHistoryBlockDetectedConsumer : IConsumer<FirstPassHistoryBlockDetected>
    {
        private readonly ILogger<FirstPassHistoryBlockDetectedConsumer> _logger;
        private readonly IBlocksRepository _blocksRepository;
        private readonly ISecondPassHistoryIndexersRepository _secondPassHistoryIndexersRepository;
        private readonly IPublishEndpoint _persistentPublisher;

        public FirstPassHistoryBlockDetectedConsumer(ILogger<FirstPassHistoryBlockDetectedConsumer> logger,
            IBlocksRepository blocksRepository,
            ISecondPassHistoryIndexersRepository secondPassHistoryIndexersRepository,
            IPublishEndpoint persistentPublisher)
        {
            _logger = logger;
            _blocksRepository = blocksRepository;
            _secondPassHistoryIndexersRepository = secondPassHistoryIndexersRepository;
            _persistentPublisher = persistentPublisher;
        }

        public async Task Consume(ConsumeContext<FirstPassHistoryBlockDetected> context)
        {
            var evt = context.Message;
            var secondPassIndexer = await _secondPassHistoryIndexersRepository.Get(evt.BlockchainId);

            var secondPassIndexingResult = await secondPassIndexer.IndexAvailableBlocks(
                // TODO: To config
                100,
                _blocksRepository,
                _persistentPublisher);

            await _secondPassHistoryIndexersRepository.Update(secondPassIndexer);

            if (secondPassIndexingResult == SecondPassHistoryIndexingResult.IndexingCompleted)
            {
                // TODO: Start ongoing indexer
            }

            _logger.LogInformation("Second-pass indexer has processed the blocks batch {@context}", secondPassIndexer);
        }
    }
}
