using System;
using GreenPipes;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Indexer.Common.Configuration;
using Indexer.Common.Domain;
using Indexer.Common.HostedServices;
using Indexer.Common.Messaging.InMemoryBus;
using Indexer.Common.Persistence;
using Indexer.Common.Telemetry;
using Indexer.Worker.HostedServices;
using Indexer.Worker.Jobs;
using Indexer.Worker.MessageConsumers;
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
            services.AddDomain();
            services.AddJobs();
            services.AddMessageConsumers();
            services.AddAppInsight(options =>
            {
                options.SetInstrumentationKey(ConfigRoot["APPINSIGHTS_INSTRUMENTATIONKEY"]);
                options.AddDefaultProperty("host-name", ApplicationEnvironment.HostName);
                options.AddDefaultProperty("app-version", ApplicationInformation.AppVersion);
            });

           
            services.AddHostedService<MigrationHost>();

            services.AddInMemoryBus((provider, cfg) =>
            {
                cfg.SetLoggerFactory(provider.GetRequiredService<ILoggerFactory>());

                cfg.ReceiveEndpoint("first-pass-block-detected", e =>
                {
                    // This is for all blockchains
                    e.UseConcurrencyLimit(8);

                    e.Consumer(provider.GetRequiredService<FirstPassBlockDetectedConsumer>);
                });
            });

            services.AddMassTransit(x =>
            {
                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    cfg.Host(Config.RabbitMq.HostUrl,
                        host =>
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
                }));

                services.AddHostedService<BusHost>();
            });

            services.AddHostedService<IndexingHost>();
        }
    }
}
