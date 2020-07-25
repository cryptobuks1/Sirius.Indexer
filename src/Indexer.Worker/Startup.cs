using System;
using GreenPipes;
using Indexer.Common.Configuration;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Indexer.Common.Domain;
using Indexer.Common.HostedServices;
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
            services.AddPersistence(Config.CommonDb.ConnectionString);
            services.AddWorkerDomain();
            services.AddJobs();
            services.AddMessageConsumers();
            services.AddAppInsight(options =>
            {
                options.SetInstrumentationKey(ConfigRoot["APPINSIGHTS_INSTRUMENTATIONKEY"]);
                options.AddDefaultProperty("host-name", ApplicationEnvironment.HostName);
                options.AddDefaultProperty("app-version", ApplicationInformation.AppVersion);
            });

            services.AddHostedService<DbVersionValidationHost>();
            services.AddHostedService<MigrationHost>();
            
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

                    cfg.SetLoggerFactory(provider.Container.GetRequiredService<ILoggerFactory>());

                    cfg.ReceiveEndpoint("sirius-indexer-publish-all-assets", e =>
                        {
                            e.Consumer(provider.Container.GetRequiredService<PublishAllAssetsConsumer>);
                        });

                    cfg.ReceiveEndpoint("sirius-indexer-publish-asset", e =>
                        {
                            e.Consumer(provider.Container.GetRequiredService<PublishAssetConsumer>);
                        });

                    cfg.ReceiveEndpoint("sirius-indexer-blockchain-updates", e =>
                        {
                            e.Consumer(provider.Container.GetRequiredService<BlockchainUpdatesConsumer>);
                        });
                }));

                services.AddHostedService<BusHost>();
            });

            services.AddHostedService<IndexingHost>();
        }
    }
}
