using System;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.Persistence.ObservedOperations;
using Indexer.Common.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Persistence
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<IAssetsRepository, AssetsRepository>();
            services.AddTransient<IBlockchainsRepository, BlockchainsRepository>();
            services.AddTransient<IObservedOperationsRepository, ObservedOperationsRepository>();
            services.AddTransient<IBlocksRepository, BlocksRepository>();
            services.AddTransient<IFirstPassIndexersRepository, FirstPassIndexersRepository>();
            services.AddTransient<ISecondPassIndexersRepository, SecondPassIndexersRepository>();
            services.AddTransient<IOngoingIndexersRepository, OngoingIndexersRepository>();
            
            services.AddSingleton<Func<DatabaseContext>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();

                optionsBuilder
                    .AddInterceptors(new DbCommandAppInsightInterceptor(x.GetRequiredService<IAppInsight>()))
                    .UseLoggerFactory(x.GetRequiredService<ILoggerFactory>())
                    .UseNpgsql(connectionString,
                    builder =>
                        builder.MigrationsHistoryTable(
                            DatabaseContext.MigrationHistoryTable,
                            DatabaseContext.SchemaName));

                DatabaseContext CreateDatabaseContext()
                {
                    return new DatabaseContext(optionsBuilder.Options, x.GetRequiredService<IAppInsight>());
                }

                return CreateDatabaseContext;
            });

            // TODO: Consider using services.AddDbContextPooling

            return services;
        }
    }
}
