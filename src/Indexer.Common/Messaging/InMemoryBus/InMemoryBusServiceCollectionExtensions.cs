using System;
using Indexer.Common.HostedServices;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Common.Messaging.InMemoryBus
{
    public static class InMemoryBusServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemoryBus(this IServiceCollection services, Action<IServiceProvider, IInMemoryBusFactoryConfigurator> options)
        {
            services.AddSingleton<IInMemoryBus, InMemoryBus>(x =>
            {
                var busControl = Bus.Factory.CreateUsingInMemory(cfg => options(x, cfg));

                return new InMemoryBus(busControl);
            });

            services.AddHostedService<InMemoryBusHost>();

            return services;
        }
    }
}
