using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.FirstPass
{
    public class FirstPassIndexingStrategyFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly IBlockReadersProvider _blockReadersProvider;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly UnspentCoinsFactory _unspentCoinsFactor;

        private readonly ConcurrentDictionary<string, BlockchainMetamodel> _blockchainMetamodelCache;

        public FirstPassIndexingStrategyFactory(ILoggerFactory loggerFactory,
            IBlockchainsRepository blockchainsRepository,
            IBlockReadersProvider blockReadersProvider,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            UnspentCoinsFactory unspentCoinsFactor)
        {
            _loggerFactory = loggerFactory;
            _blockchainsRepository = blockchainsRepository;
            _blockReadersProvider = blockReadersProvider;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _unspentCoinsFactor = unspentCoinsFactor;

            _blockchainMetamodelCache = new ConcurrentDictionary<string, BlockchainMetamodel>();
        }

        public async Task<IFirstPasseIndexingStrategy> Create(string blockchainId)
        {
            var blockchainMetamodel = await GetBlockchainMetamodel(blockchainId);
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
                    return new NonceFirstPassIndexingStrategy();

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockchainMetamodel.Protocol.DoubleSpendingProtectionType), blockchainMetamodel.Protocol.DoubleSpendingProtectionType, null);
            }
        }

        private async Task<BlockchainMetamodel> GetBlockchainMetamodel(string blockchainId)
        {
            if (_blockchainMetamodelCache.ContainsKey(blockchainId))
            {
                return _blockchainMetamodelCache[blockchainId];
            }

            var blockchainMetamodel = await _blockchainsRepository.GetAsync(blockchainId);

            if (!_blockchainMetamodelCache.TryAdd(blockchainId, blockchainMetamodel))
            {
                return _blockchainMetamodelCache[blockchainId];
            }

            return blockchainMetamodel;
        }
    }
}
