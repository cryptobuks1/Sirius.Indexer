using System.Threading;
using System.Threading.Tasks;
using Indexer.Bilv1.Repositories.DbContexts;
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
        private readonly DbContextOptionsBuilder<IndexerBilV1Context> _bilV1ContextOptions;

        public MigrationHost(ILogger<MigrationHost> logger, 
            DbContextOptionsBuilder<DatabaseContext> contextOptions,
            DbContextOptionsBuilder<IndexerBilV1Context> bilV1ContextOptions)
        {
            _logger = logger;
            _contextOptions = contextOptions;
            _bilV1ContextOptions = bilV1ContextOptions;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EF Migration is being started...");

            await using var context = new DatabaseContext(_contextOptions.Options);

            await context.Database.MigrateAsync(cancellationToken);

            await using var bilV1Context = new IndexerBilV1Context(_bilV1ContextOptions.Options);
            await bilV1Context.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("EF Migration has been completed.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
