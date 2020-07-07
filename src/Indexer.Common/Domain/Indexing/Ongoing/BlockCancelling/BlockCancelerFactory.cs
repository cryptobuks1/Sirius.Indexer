using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blockchains;
using Indexer.Common.Persistence;
using MassTransit;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling
{
    public sealed class BlockCancelerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBlockchainMetamodelProvider _blockchainMetamodelProvider;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly IPublishEndpoint _publisher;

        public BlockCancelerFactory(ILoggerFactory loggerFactory,
            IBlockchainMetamodelProvider blockchainMetamodelProvider,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            IPublishEndpoint publisher)
        {
            _loggerFactory = loggerFactory;
            _blockchainMetamodelProvider = blockchainMetamodelProvider;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _publisher = publisher;
        }

        public async Task<IBlockCanceler> Create(string blockchainId)
        {
            var blockchainMetamodel = await _blockchainMetamodelProvider.Get(blockchainId);

            switch (blockchainMetamodel.Protocol.DoubleSpendingProtectionType)
            {
                case DoubleSpendingProtectionType.Coins:
                    return new CoinsBlockCanceler(
                        _loggerFactory.CreateLogger<CoinsBlockCanceler>(),
                        _blockchainDbUnitOfWorkFactory,
                        _publisher);

                case DoubleSpendingProtectionType.Nonce:
                    return new NonceBlockCanceler();

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockchainMetamodel.Protocol.DoubleSpendingProtectionType), blockchainMetamodel.Protocol.DoubleSpendingProtectionType, null);
            }
        }
    }
}
