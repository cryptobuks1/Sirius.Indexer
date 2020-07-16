﻿using System;
using Indexer.Common.Configuration;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Indexer.Common.HostedServices;
using Indexer.Common.Persistence;
using Indexer.Common.ServiceFunctions;
using Indexer.GrpcServices;
using Swisschain.Sdk.Server.Common;

namespace Indexer
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            base.ConfigureServicesExt(services);

            services.AddPersistence(Config.CommonDb.ConnectionString);

            services.AddMassTransit(x =>
            {
                EndpointConvention.Map<PublishAllAssets>(new Uri("queue:sirius-indexer-publish-all-assets"));
                EndpointConvention.Map<PublishAsset>(new Uri("queue:sirius-indexer-publish-asset"));

                x.AddBus(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    cfg.Host(Config.RabbitMq.HostUrl, host =>
                    {
                        host.Username(Config.RabbitMq.Username);
                        host.Password(Config.RabbitMq.Password);
                    });

                    cfg.SetLoggerFactory(provider.GetRequiredService<ILoggerFactory>());
                }));

                services.AddHostedService<BusHost>();
            });
        }

        protected override void RegisterEndpoints(IEndpointRouteBuilder endpoints)
        {
            base.RegisterEndpoints(endpoints);

            endpoints.MapGrpcService<MonitoringService>();
            endpoints.MapGrpcService<ObservedOperationService>();
            endpoints.MapGrpcService<UnspentCoinsService>();
            endpoints.MapGrpcService<NoncesService>();
        }
    }
}
