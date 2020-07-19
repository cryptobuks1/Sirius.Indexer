using System;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.HostedServices
{
    public class DbVersionValidationHost : IHostedService
    {
        private readonly ILogger<DbVersionValidationHost> _logger;
        private readonly IDbVersionValidator _dbVersionValidator;

        public DbVersionValidationHost(ILogger<DbVersionValidationHost> logger, IDbVersionValidator dbVersionValidator)
        {
            _logger = logger;
            _dbVersionValidator = dbVersionValidator;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _dbVersionValidator.Validate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate DB version");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
