using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Ongoing;
using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Common.Domain
{
    public static class DomainServiceCollectionExtensions
    {
        public static IServiceCollection AddDomain(this IServiceCollection services)
        {
            services.AddTransient<ChainWalker>();
            services.AddSingleton<IBlockReadersProvider, BlockReadersProvider>();

            return services;
        }
    }
}
