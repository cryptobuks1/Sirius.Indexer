using System;
using GreenPipes;
using Indexer.Bilv1.Repositories;
using Indexer.Bilv1.Repositories.DbContexts;
using Indexer.Common.Bilv1.DomainServices;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Indexer.Common.Configuration;
using Indexer.Common.HostedServices;
using Indexer.Common.Persistence;
using Indexer.Worker.HostedServices;
using Indexer.Worker.MessageConsumers;
using Microsoft.EntityFrameworkCore;
using Swisschain.Sdk.Server.Common;

namespace Indexer.Worker
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            base.ConfigureServicesExt(services);

            services.AddHttpClient();
            services.AddPersistence(Config.Db.ConnectionString);
            services.AddMessageConsumers();


            services.AddBilV1Repositories();
            services.AddBilV1Services();
            services.AddBilV1();
            services.AddHostedService<MigrationHost>();
            services.AddHostedService<BalanceProcessorsHost>();

            services.AddMessageConsumers();

            services.AddSingleton<DbContextOptionsBuilder<IndexerBilV1Context>>(x =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<IndexerBilV1Context>();
                optionsBuilder.UseNpgsql(this.Config.Db.ConnectionString,
                    builder =>
                        builder.MigrationsHistoryTable(
                            PostgresBilV1RepositoryConfiguration.MigrationHistoryTable,
                            PostgresBilV1RepositoryConfiguration.SchemaName));

                return optionsBuilder;
            });


            services.AddMassTransit(x =>
            {
                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    cfg.Host(Config.RabbitMq.HostUrl, host =>
                    {
                        host.Username(Config.RabbitMq.Username);
                        host.Password(Config.RabbitMq.Password);
                    });

                    cfg.UseMessageRetry(y =>
                        y.Exponential(5,
                            TimeSpan.FromMilliseconds(100),
                            TimeSpan.FromMilliseconds(10_000),
                            TimeSpan.FromMilliseconds(100)));

                    cfg.SetLoggerFactory(provider.GetRequiredService<ILoggerFactory>());

                    cfg.ReceiveEndpoint("sirius-indexer-publish-all-assets", e =>
                    {
                        e.Consumer(provider.GetRequiredService<PublishAllAssetsConsumer>);
                    });

                    cfg.ReceiveEndpoint("sirius-indexer-publish-asset", e =>
                    {
                        e.Consumer(provider.GetRequiredService<PublishAssetConsumer>);
                    });
                    
                    cfg.ReceiveEndpoint("sirius-indexer-blockchain-updates", e =>
                    {
                        e.Consumer(provider.GetRequiredService<BlockchainUpdatesConsumer>);
                    });

                    cfg.ReceiveEndpoint("sirius-indexer-wallet-added", e =>
                    {
                        e.Consumer(provider.GetRequiredService<WalletAddedConsumer>);
                    });
                }));

                services.AddHostedService<BusHost>();
            });
        }
    }
}
