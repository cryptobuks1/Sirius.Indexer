using System;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Persistence.BlockchainDbMigrations;
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
        private readonly AppConfig _config;
        private readonly IBlockchainDbMigrationsManager _blockchainDbMigrationsManager;

        public MigrationHost(ILogger<MigrationHost> logger, 
            Func<CommonDatabaseContext> contextFactory,
            AppConfig config,
            IBlockchainDbMigrationsManager blockchainDbMigrationsManager)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _config = config;
            _blockchainDbMigrationsManager = blockchainDbMigrationsManager;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("DB schema migration is being started...");

                await using var context = _contextFactory.Invoke();

                await context.Database.MigrateAsync(cancellationToken);

                foreach (var (blockchainId, _) in _config.Blockchains)
                {
                    await _blockchainDbMigrationsManager.Migrate(blockchainId);
                }

                _logger.LogInformation("DB schema migration has been completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute DB schema migration");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
