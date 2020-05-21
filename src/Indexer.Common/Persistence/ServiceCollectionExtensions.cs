using Indexer.Common.Domain;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Persistence.DbContexts;
using Indexer.Common.Persistence.ObservedOperations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Persistence
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<IAssetsRepository, AssetsRepository>();
            services.AddTransient<IBlockchainsRepository, BlockchainsRepository>();
            services.AddTransient<IObservedOperationsRepository, ObservedOperationsRepository>();

            services.AddSingleton<IFirstPassHistoryIndexersRepository, InMemoryFirstPassHistoryIndexersRepository>();
            services.AddSingleton<ISecondPassHistoryIndexersRepository, InMemorySecondPassHistoryIndexersRepository>();
            services.AddSingleton<IOngoingIndexersRepository, InMemoryOngoingIndexersRepository>();
            services.AddSingleton<IBlocksRepository, InMemoryBlocksRepository>();

            services.AddSingleton<DbContextOptionsBuilder<DatabaseContext>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();

                optionsBuilder
                    .UseLoggerFactory(x.GetRequiredService<ILoggerFactory>())
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
