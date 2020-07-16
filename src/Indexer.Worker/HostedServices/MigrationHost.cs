using System;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.HostedServices
{
    public class MigrationHost : IHostedService
    {
        private readonly ILogger<MigrationHost> _logger;
        private readonly Func<CommonDatabaseContext> _contextFactory;

        public MigrationHost(ILogger<MigrationHost> logger, 
            Func<CommonDatabaseContext> contextFactory)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EF Migration is being started...");

            await using var context = _contextFactory.Invoke();

            await context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("EF Migration has been completed.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
