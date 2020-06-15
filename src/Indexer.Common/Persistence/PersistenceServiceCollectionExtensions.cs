﻿using System;
using System.Data;
using System.Threading.Tasks;
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
using Npgsql;

namespace Indexer.Common.Persistence
{
    public static class PersistenceServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddTransient<IBlockchainsRepository, BlockchainsRepository>();
            services.AddTransient<IObservedOperationsRepository, ObservedOperationsRepository>();
            services.AddTransient<IAssetsRepository>(c => 
                new AssetsRepositoryCacheDecorator(
                    new AssetsRepositoryRetryDecorator(
                        new AssetsRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>()))));
            services.AddTransient<IBlockHeadersRepository>(c =>
                new BlockHeadersRepositoryRetryDecorator(
                    new BlockHeadersRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>(), c.GetRequiredService<IAppInsight>())));
            services.AddTransient<ITransactionHeadersRepository>(c =>
                new TransactionHeadersRepositoryRetryDecorator(
                    new TransactionHeadersRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>(), c.GetRequiredService<IAppInsight>())));
            services.AddTransient<IInputCoinsRepository>(c =>
                new InputCoinsRepositoryRetryDecorator(
                    new InputCoinsRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>())));
            services.AddTransient<IUnspentCoinsRepository>(c =>
                new UnspentCoinsRepositoryRetryDecorator(
                    new UnspentCoinsRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>())));
            services.AddTransient<ISpentCoinsRepository>(c =>
                new SpentCoinsRepositoryRetryDecorator(
                    new SpentCoinsRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>())));
            services.AddTransient<IBalanceUpdatesRepository>(c =>
                new BalanceUpdatesRepositoryRetryDecorator(
                    new BalanceUpdatesRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>())));
            services.AddTransient<IFeesRepository>(c =>
                new FeesRepositoryRetryDecorator(
                    new FeesRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>())));
            services.AddTransient<IFirstPassIndexersRepository>(c => 
                new FirstPassIndexersRepositoryRetryDecorator(
                    new FirstPassIndexersRepository(c.GetRequiredService<Func<DatabaseContext>>())));
            services.AddTransient<ISecondPassIndexersRepository>(c =>
                new SecondPassIndexersRepositoryRetryDecorator(
                    new SecondPassIndexersRepository(c.GetRequiredService<Func<DatabaseContext>>())));
            services.AddTransient<IOngoingIndexersRepository>(c =>
                new OngoingIndexersRepositoryRetryDecorator(
                    new OngoingIndexersRepository(c.GetRequiredService<Func<DatabaseContext>>())));

            services.AddTransient<IBlockchainSchemaBuilder, BlockchainSchemaBuilder>();
            services.AddTransient<IDbVersionValidator, DbVersionValidator>();

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

            services.AddSingleton<Func<Task<NpgsqlConnection>>>(x =>
            {
                async Task<NpgsqlConnection> CreateConnection()
                {
                    var connection = new NpgsqlConnection(connectionString);

                    if (connection.State != ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    return connection;
                }

                return CreateConnection;
            });

            // TODO: Consider using services.AddDbContextPooling

            return services;
        }
    }
}