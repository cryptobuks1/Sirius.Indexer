using Indexer.Bilv1.Domain.Services;
using Indexer.Common.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Indexer.Common.Bilv1.DomainServices
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBilV1Services(this IServiceCollection services)
        {
            services.AddTransient<IAssetService, AssetService>();
            services.AddTransient<IWalletsService, WalletsService>();

            services.AddSingleton<BlockchainApiClientProvider>(c => new BlockchainApiClientProvider(
                c.GetRequiredService<ILoggerFactory>(),
                c.GetRequiredService <IBlockchainsRepository>()));

            return services;
        }
    }
}
