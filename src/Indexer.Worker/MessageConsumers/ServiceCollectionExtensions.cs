using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Worker.MessageConsumers
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMessageConsumers(this IServiceCollection services)
        {
            services.AddTransient<PublishAllAssetsConsumer>();
            services.AddTransient<PublishAssetConsumer>();
            services.AddTransient<BlockchainUpdatesConsumer>();
            services.AddTransient<FirstPassBlockUpdatesConsumer>();

            return services;
        }
    }
}
