using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.HostedServices
{
    public class BusHost : IHostedService
    {
        private readonly IBusControl _busControl;
        private readonly ILogger<BusHost> _logger;

        public BusHost(IBusControl busControl, ILogger<BusHost> logger)
        {
            _busControl = busControl;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Bus host being started...");

                await _busControl.StartAsync(cancellationToken);

                _logger.LogInformation("Bus host has been started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Bus");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Bus host being stopped...");

                await _busControl.StopAsync(cancellationToken);

                _logger.LogInformation("Bus host has been stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop Bus");
            }
        }
    }
}
