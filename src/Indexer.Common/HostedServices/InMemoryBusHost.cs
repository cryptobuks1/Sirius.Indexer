using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Messaging.InMemoryBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.HostedServices
{
    public class InMemoryBusHost : IHostedService
    {
        private readonly IInMemoryBus _inMemoryBus;
        private readonly ILogger<InMemoryBusHost> _logger;

        public InMemoryBusHost(IInMemoryBus inMemoryBus, ILogger<InMemoryBusHost> logger)
        {
            _inMemoryBus = inMemoryBus;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("In-memory bus host being started...");

            await _inMemoryBus.StartAsync(cancellationToken);

            _logger.LogInformation("In-memory bus host has been started");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("In-memory bus host being stopped...");

            await _inMemoryBus.StopAsync(cancellationToken);

            _logger.LogInformation("In-memory bus host has been stopped");
        }
    }
}
