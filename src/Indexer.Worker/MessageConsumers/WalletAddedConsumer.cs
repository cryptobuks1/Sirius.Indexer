using System.Threading.Tasks;
using Indexer.Bilv1.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.VaultAgent.MessagingContract;

namespace Indexer.Worker.MessageConsumers
{
    public class WalletAddedConsumer : IConsumer<WalletAdded>
    {
        private readonly ILogger<WalletAddedConsumer> _logger;
        private readonly IWalletsService _walletService;

        public WalletAddedConsumer(ILogger<WalletAddedConsumer> logger, IWalletsService walletService)
        {
            _logger = logger;
            _walletService = walletService;
        }

        public async Task Consume(ConsumeContext<WalletAdded> context)
        {
            var evt = context.Message;

            await _walletService.ImportWalletAsync(evt.BlockchainId, evt.Address);

            _logger.LogInformation("WalletAdded event has been processed. {@context}", evt);
        }
    }
}
