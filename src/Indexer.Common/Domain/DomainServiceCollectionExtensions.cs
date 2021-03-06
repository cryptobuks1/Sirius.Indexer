﻿using Indexer.Common.Domain.Assets;
using Indexer.Common.Domain.Blockchains;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Domain.Indexing.FirstPass;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling;
using Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing;
using Microsoft.Extensions.DependencyInjection;
using Swisschain.Sirius.Sdk.Crypto.AddressFormatting;

namespace Indexer.Common.Domain
{
    public static class DomainServiceCollectionExtensions
    {
        public static IServiceCollection AddApiDomain(this IServiceCollection services)
        {
            services.AddSingleton<IBlockchainMetamodelProvider, BlockchainMetamodelProvider>();

            return services;
        }

        public static IServiceCollection AddWorkerDomain(this IServiceCollection services)
        {
            services.AddTransient<ChainWalker>();
            services.AddSingleton<IBlockReadersProvider, BlockReadersProvider>();
            services.AddSingleton<IBlockchainMetamodelProvider, BlockchainMetamodelProvider>();
            services.AddTransient<AssetsManager>();
            services.AddTransient<UnspentCoinsFactory>();
            services.AddTransient<FirstPassIndexingStrategyFactory>();
            services.AddTransient<OngoingIndexingStrategyFactory>();
            services.AddTransient<BlockCancelerFactory>();
            services.AddTransient<NonceBlockAssetsProvider>();
            services.AddTransient<IAddressFormatterFactory, AddressFormatterFactory>();

            return services;
        }
    }
}
