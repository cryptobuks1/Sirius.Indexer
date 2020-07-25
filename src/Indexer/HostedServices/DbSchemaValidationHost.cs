using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Indexer.Common.Persistence.BlockchainDbMigrations;
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
        private readonly AppConfig _config;
        private readonly IBlockchainDbMigrationsManager _blockchainDbMigrationsManager;

        public DbSchemaValidationHost(ILogger<DbSchemaValidationHost> logger, 
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
                _logger.LogInformation("DB schema validation is being started...");

                await using var context = _contextFactory.Invoke();

                var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);

                if (pendingMigrations.Any())
                {
                    throw new InvalidOperationException("There are pending migrations, try again later");
                }

                foreach (var (blockchainId, _) in _config.Blockchains)
                {
                    await _blockchainDbMigrationsManager.Validate(blockchainId);
                }

                _logger.LogInformation("DB schema validation has been completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate DB schema");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
