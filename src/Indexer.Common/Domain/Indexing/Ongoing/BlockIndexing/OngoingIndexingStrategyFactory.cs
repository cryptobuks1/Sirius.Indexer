using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blockchains;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    public sealed class OngoingIndexingStrategyFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlockchainMetamodelProvider _blockchainMetamodelProvider;
        private readonly IBlockReadersProvider _blockReadersProvider;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly UnspentCoinsFactory _unspentCoinsFactory;
        private readonly IPublishEndpoint _publisher;
        private readonly NonceBlockAssetsProvider _nonceBlockAssetsProvider;

        public OngoingIndexingStrategyFactory(ILoggerFactory loggerFactory,
            IBlockchainMetamodelProvider blockchainMetamodelProvider,
            IBlockReadersProvider blockReadersProvider,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            UnspentCoinsFactory unspentCoinsFactory,
            IPublishEndpoint publisher,
            NonceBlockAssetsProvider nonceBlockAssetsProvider)
        {
            _loggerFactory = loggerFactory;
            _blockchainMetamodelProvider = blockchainMetamodelProvider;
            _blockReadersProvider = blockReadersProvider;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _unspentCoinsFactory = unspentCoinsFactory;
            _publisher = publisher;
            _nonceBlockAssetsProvider = nonceBlockAssetsProvider;
        }

        public async Task<IOngoingIndexingStrategy> Create(string blockchainId)
        {
            var blockchainMetamodel = await _blockchainMetamodelProvider.Get(blockchainId);
            var blocksReader = await _blockReadersProvider.Get(blockchainId);

            switch (blockchainMetamodel.Protocol.DoubleSpendingProtectionType)
            {
                case DoubleSpendingProtectionType.Coins:
                    return new CoinsOngoingIndexingStrategy(
                        _loggerFactory,
                        blocksReader,
                        _blockchainDbUnitOfWorkFactory,
                        _unspentCoinsFactory,
                        _publisher);

                case DoubleSpendingProtectionType.Nonce:
                    return new NonceOngoingIndexingStrategy(
                        blocksReader,
                        _blockchainDbUnitOfWorkFactory,
                        _nonceBlockAssetsProvider,
                        _publisher);
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(blockchainMetamodel.Protocol.DoubleSpendingProtectionType), blockchainMetamodel.Protocol.DoubleSpendingProtectionType, null);
            }
        }
    }
}
