using System;
using Indexer.Common.Persistence.Entities.Assets;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.Persistence.Entities.FirstPassIndexers;
using Indexer.Common.Persistence.Entities.OngoingIndexers;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.Persistence.EntityFramework;
using Indexer.Common.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Persistence
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string commonConnectionString)
        {
            services.AddTransient<IBlockchainsRepository, BlockchainsRepository>();
            services.AddTransient<IAssetsRepository>(c => 
                new AssetsRepositoryCacheDecorator(
                    new AssetsRepositoryRetryDecorator(
                        new AssetsRepository(c.GetRequiredService<Func<CommonDatabaseContext>>()))));
            services.AddTransient<IFirstPassIndexersRepository>(c => 
                new FirstPassIndexersRepositoryRetryDecorator(
                    new FirstPassIndexersRepository(c.GetRequiredService<Func<CommonDatabaseContext>>())));
            services.AddTransient<ISecondPassIndexersRepository>(c =>
                new SecondPassIndexersRepositoryRetryDecorator(
                    new SecondPassIndexersRepository(c.GetRequiredService<Func<CommonDatabaseContext>>())));
            services.AddTransient<IOngoingIndexersRepository>(c =>
                new OngoingIndexersRepositoryRetryDecorator(
                    new OngoingIndexersRepository(c.GetRequiredService<Func<CommonDatabaseContext>>())));

            services.AddTransient<IBlockchainSchemaBuilder, BlockchainSchemaBuilder>();
            services.AddTransient<IDbVersionValidator, DbVersionValidator>();

            services.AddSingleton<Func<CommonDatabaseContext>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<CommonDatabaseContext>();

                optionsBuilder
                    .AddInterceptors(new DbCommandAppInsightInterceptor(x.GetRequiredService<IAppInsight>()))
                    .UseLoggerFactory(x.GetRequiredService<ILoggerFactory>())
                    .UseNpgsql(commonConnectionString,
                    builder =>
                        builder.MigrationsHistoryTable(
                            CommonDatabaseContext.MigrationHistoryTable,
                            CommonDatabaseContext.SchemaName));

                CommonDatabaseContext CreateDatabaseContext()
                {
                    return new CommonDatabaseContext(optionsBuilder.Options, x.GetRequiredService<IAppInsight>());
                }
                
                return CreateDatabaseContext;
            });

            services.AddSingleton<IBlockchainDbConnectionFactory, BlockchainDbConnectionFactory>();
            services.AddSingleton<IBlockchainDbUnitOfWorkFactory, BlockchainDbUnitOfWorkFactory>();

            // TODO: Consider using services.AddDbContextPooling

            return services;
        }
    }
}
