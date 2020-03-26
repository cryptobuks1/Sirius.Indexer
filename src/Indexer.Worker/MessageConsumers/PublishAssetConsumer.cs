using System.Threading.Tasks;
using Indexer.Common.ServiceFunctions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.MessagingContract;

namespace Indexer.Worker.MessageConsumers
{
    public class PublishAssetConsumer : IConsumer<PublishAsset>
    {
        private readonly ILogger<PublishAssetConsumer> _logger;

        public PublishAssetConsumer(ILogger<PublishAssetConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PublishAsset> context)
        {
            var command = context.Message;

            _logger.LogInformation("Asset being published {@context}", command);

            await context.Publish(new AssetAdded
            {
                AssetId = command.AssetId,
                BlockchainId = command.BlockchainId,
                Symbol = command.Symbol,
                Address = command.Address,
                Accuracy = command.Accuracy
            });

            await Task.CompletedTask;
        }
    }
}
