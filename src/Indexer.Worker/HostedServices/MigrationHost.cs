using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Indexer.Worker.HostedServices
{
    public class MigrationHost : IHostedService
    {
        private readonly ILogger<MigrationHost> _logger;
        private readonly DbContextOptionsBuilder<DatabaseContext> _contextOptions;

        public MigrationHost(ILogger<MigrationHost> logger, 
            DbContextOptionsBuilder<DatabaseContext> contextOptions)
        {
            _logger = logger;
            _contextOptions = contextOptions;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EF Migration is being started...");

            await using var context = new DatabaseContext(_contextOptions.Options);

            await context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("EF Migration has been completed.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
