using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blockchains;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    public class FirstPassIndexingStrategyFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlockReadersProvider _blockReadersProvider;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly UnspentCoinsFactory _unspentCoinsFactor;
        private readonly IBlockchainMetamodelProvider _blockchainMetamodelProvider;
        private readonly NonceFeesFactory _nonceFeesFactory;
        private readonly NonceBalanceUpdatesCalculator _nonceBalanceUpdatesCalculator;

        public FirstPassIndexingStrategyFactory(ILoggerFactory loggerFactory,
            IBlockReadersProvider blockReadersProvider,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            UnspentCoinsFactory unspentCoinsFactor,
            IBlockchainMetamodelProvider blockchainMetamodelProvider,
            NonceFeesFactory nonceFeesFactory,
            NonceBalanceUpdatesCalculator nonceBalanceUpdatesCalculator)
        {
            _loggerFactory = loggerFactory;
            _blockReadersProvider = blockReadersProvider;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _unspentCoinsFactor = unspentCoinsFactor;
            _blockchainMetamodelProvider = blockchainMetamodelProvider;
            _nonceFeesFactory = nonceFeesFactory;
            _nonceBalanceUpdatesCalculator = nonceBalanceUpdatesCalculator;
        }

        public async Task<IFirstPasseIndexingStrategy> Create(string blockchainId)
        {
            var blockchainMetamodel = await _blockchainMetamodelProvider.Get(blockchainId);
            var blocksReader = await _blockReadersProvider.Get(blockchainId);

            switch (blockchainMetamodel.Protocol.DoubleSpendingProtectionType)
            {
                case DoubleSpendingProtectionType.Coins:
                    return new CoinsFirstPassIndexingStrategy(
                        _loggerFactory.CreateLogger<CoinsFirstPassIndexingStrategy>(),
                        blocksReader,
                        _blockchainDbUnitOfWorkFactory,
                        _unspentCoinsFactor);

                case DoubleSpendingProtectionType.Nonce:
                    return new NonceFirstPassIndexingStrategy(
                        _loggerFactory.CreateLogger<NonceFirstPassIndexingStrategy>(),
                        blocksReader, 
                        _nonceFeesFactory,
                        _nonceBalanceUpdatesCalculator,
                        _blockchainDbUnitOfWorkFactory);

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockchainMetamodel.Protocol.DoubleSpendingProtectionType), blockchainMetamodel.Protocol.DoubleSpendingProtectionType, null);
            }
        }
    }
}
