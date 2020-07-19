using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.HostedServices
{
    public class DbSchemaValidationHost : IHostedService
    {
        private readonly ILogger<DbSchemaValidationHost> _logger;
        private readonly Func<CommonDatabaseContext> _contextFactory;

        public DbSchemaValidationHost(ILogger<DbSchemaValidationHost> logger, 
            Func<CommonDatabaseContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("EF Schema validation is being started...");

                await using var context = _contextFactory.Invoke();

                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);

                if (pendingMigrations.Any())
                {
                    throw new InvalidOperationException("There are pending migrations, try again later");
                }

                _logger.LogInformation("EF Schema validation has been completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate EF schema");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
