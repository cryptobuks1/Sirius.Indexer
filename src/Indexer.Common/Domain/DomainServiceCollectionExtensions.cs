using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Domain.Transactions.Transfers;
using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Common.Domain
{
    public static class DomainServiceCollectionExtensions
    {
        public static IServiceCollection AddDomain(this IServiceCollection services)
        {
            services.AddTransient<ChainWalker>();
            services.AddSingleton<IBlockReadersProvider, BlockReadersProvider>();
            services.AddTransient<AssetsManager>();
            services.AddTransient<UnspentCoinsFactory>();
            services.AddTransient<CoinsBlockApplier>();
            services.AddTransient<CoinsBlockCanceler>();

            return services;
        }
    }
}
