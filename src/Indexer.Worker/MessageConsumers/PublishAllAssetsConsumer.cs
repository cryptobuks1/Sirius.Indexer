using System.Threading.Tasks;
using Indexer.Common.Persistence.Entities.Assets;
using Indexer.Common.ServiceFunctions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.MessagingContract;

namespace Indexer.Worker.MessageConsumers
{
    public class PublishAllAssetsConsumer : IConsumer<PublishAllAssets>
    {
        private readonly ILogger<PublishAllAssetsConsumer> _logger;
        private readonly IAssetsRepository _assetsRepository;

        public PublishAllAssetsConsumer(ILogger<PublishAllAssetsConsumer> logger,
            IAssetsRepository assetsRepository)
        {
            _logger = logger;
            _assetsRepository = assetsRepository;
        }

        public async Task Consume(ConsumeContext<PublishAllAssets> context)
        {
            var command = context.Message;

            _logger.LogInformation("All assets being published {@context}", command);

            var assets = await _assetsRepository.GetAllAsync(command.BlockchainId);

            foreach (var asset in assets)
            {
                await context.Publish(new AssetAdded
                {
                    AssetId = asset.Id,
                    BlockchainId = asset.BlockchainId,
                    Symbol = asset.Symbol,
                    Address = asset.Address,
                    Accuracy = asset.Accuracy
                });
            }

            await Task.CompletedTask;
        }
    }
}
