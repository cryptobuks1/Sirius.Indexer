using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Indexer.Common.ReadModel.Blockchains;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Integrations.MessagingContract;

namespace Indexer.Worker.MessageConsumers
{
    public class BlockchainUpdatesConsumer : IConsumer<BlockchainAdded>
    {
        private readonly ILogger<BlockchainUpdatesConsumer> _logger;
        private readonly IBlockchainsRepository blockchainsRepository;

        public BlockchainUpdatesConsumer(
            ILogger<BlockchainUpdatesConsumer> logger,
            IBlockchainsRepository blockchainsRepository)
        {
            _logger = logger;
            this.blockchainsRepository = blockchainsRepository;
        }

        public async Task Consume(ConsumeContext<BlockchainAdded> context)
        {
            var evt = context.Message;

            var model = new Blockchain
            {
                BlockchainId = evt.BlockchainId,
                IntegrationUrl = evt.IntegrationUrl.ToString()
            };

            await blockchainsRepository.AddOrReplaceAsync(model);

            _logger.LogInformation("BlockchainAdded command has been processed {@context}", evt);
        }
    }
}
