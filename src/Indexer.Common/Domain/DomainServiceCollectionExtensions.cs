using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Blockchains;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.FirstPass;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling;
using Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing;
using Indexer.Common.Domain.Transactions.Transfers.Coins;
using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Common.Domain
{
    public static class DomainServiceCollectionExtensions
    {
        public static IServiceCollection AddDomain(this IServiceCollection services)
        {
            services.AddTransient<ChainWalker>();
            services.AddSingleton<IBlockReadersProvider, BlockReadersProvider>();
            services.AddSingleton<IBlockchainMetamodelProvider, BlockchainMetamodelProvider>();
            services.AddTransient<AssetsManager>();
            services.AddTransient<UnspentCoinsFactory>();
            services.AddTransient<FirstPassIndexingStrategyFactory>();
            services.AddTransient<OngoingIndexingStrategyFactory>();
            services.AddTransient<BlockCancelerFactory>();

            return services;
        }
    }
}
