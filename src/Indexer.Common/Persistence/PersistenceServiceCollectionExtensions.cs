using System;
using Indexer.Common.Persistence.Entities.Assets;
using Indexer.Common.Persistence.Entities.BalanceUpdates;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.Fees;
using Indexer.Common.Persistence.Entities.FirstPassIndexers;
using Indexer.Common.Persistence.Entities.InputCoins;
using Indexer.Common.Persistence.Entities.ObservedOperations;
using Indexer.Common.Persistence.Entities.OngoingIndexers;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.Persistence.Entities.SpentCoins;
using Indexer.Common.Persistence.Entities.TransactionHeaders;
using Indexer.Common.Persistence.Entities.UnspentCoins;
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

            services.AddTransient<IObservedOperationsRepository>(c =>
                new ObservedOperationsRepositoryRetryDecorator(
                    new ObservedOperationsRepository(c.GetRequiredService<IBlockchainDbConnectionFactory>())));
            services.AddTransient<IBlockHeadersRepository>(c =>
                new BlockHeadersRepositoryRetryDecorator(
                    new BlockHeadersRepository(c.GetRequiredService<IBlockchainDbConnectionFactory>(), c.GetRequiredService<IAppInsight>())));
            services.AddTransient<ITransactionHeadersRepository>(c =>
                new TransactionHeadersRepositoryRetryDecorator(
                    new TransactionHeadersRepository(c.GetRequiredService<IBlockchainDbConnectionFactory>(), c.GetRequiredService<IAppInsight>())));
            services.AddTransient<IInputCoinsRepository>(c =>
                new InputCoinsRepositoryRetryDecorator(
                    new InputCoinsRepository(c.GetRequiredService<IBlockchainDbConnectionFactory>())));
            services.AddTransient<IUnspentCoinsRepository>(c =>
                new UnspentCoinsRepositoryRetryDecorator(
                    new UnspentCoinsRepository(c.GetRequiredService<IBlockchainDbConnectionFactory>())));
            services.AddTransient<ISpentCoinsRepository>(c =>
                new SpentCoinsRepositoryRetryDecorator(
                    new SpentCoinsRepository(c.GetRequiredService<IBlockchainDbConnectionFactory>())));
            services.AddTransient<IBalanceUpdatesRepository>(c =>
                new BalanceUpdatesRepositoryRetryDecorator(
                    new BalanceUpdatesRepository(c.GetRequiredService<IBlockchainDbConnectionFactory>())));
            services.AddTransient<IFeesRepository>(c =>
                new FeesRepositoryRetryDecorator(
                    new FeesRepository(c.GetRequiredService<IBlockchainDbConnectionFactory>())));
            
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

            // TODO: Consider using services.AddDbContextPooling

            return services;
        }
    }
}
