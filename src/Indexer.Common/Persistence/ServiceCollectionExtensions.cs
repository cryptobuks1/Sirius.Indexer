using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.Persistence.ObservedOperations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Npgsql.Logging;

namespace Indexer.Common.Persistence
{
    public static class ServiceCollectionExtensions
    {
        public static readonly ILoggerFactory MyLoggerFactory
            = LoggerFactory.Create(builder => { builder.AddConsole(); });
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IAssetsRepository, AssetsRepository>();
            services.AddSingleton<IBlockchainsRepository, BlockchainsRepository>();
            services.AddSingleton<IObservedOperationsRepository, ObservedOperationsRepository>();

            services.AddSingleton<DbContextOptionsBuilder<DatabaseContext>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
                optionsBuilder
                    //.UseLoggerFactory(MyLoggerFactory)
                    .UseNpgsql(connectionString,
                    builder =>
                        builder.MigrationsHistoryTable(
                            DatabaseContext.MigrationHistoryTable,
                            DatabaseContext.SchemaName));

                return optionsBuilder;
            });

            return services;
        }
    }
}
