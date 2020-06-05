using System;
using System.Data;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Persistence.EntityFramework;
using Indexer.Common.Persistence.ObservedOperations;
using Indexer.Common.Persistence.RetryDecorators;
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
            services.AddTransient<IAssetsRepository, AssetsRepository>();
            services.AddTransient<IBlockchainsRepository, BlockchainsRepository>();
            services.AddTransient<IObservedOperationsRepository, ObservedOperationsRepository>();
            services.AddTransient<IBlockHeadersRepository>(c =>
                new BlockHeadersRepositoryRetryDecorator(
                    new BlockHeadersRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>(), c.GetRequiredService<IAppInsight>())));
            services.AddTransient<ITransactionHeadersRepository>(c =>
                new TransactionHeadersRepositoryRetryDecorator(
                    new TransactionHeadersRepository(c.GetRequiredService<Func<Task<NpgsqlConnection>>>(), c.GetRequiredService<IAppInsight>())));
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
