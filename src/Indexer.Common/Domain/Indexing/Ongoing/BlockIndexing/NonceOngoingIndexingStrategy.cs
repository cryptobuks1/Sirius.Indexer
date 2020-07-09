using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Persistence;
using MassTransit;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    internal sealed class NonceOngoingIndexingStrategy : IOngoingIndexingStrategy
    {
        private readonly IBlocksReader _blocksReader;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly NonceBlockAssetsProvider _blockAssetsProvider;
        private readonly IPublishEndpoint _publisher;

        public NonceOngoingIndexingStrategy(IBlocksReader blocksReader, 
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            NonceBlockAssetsProvider blockAssetsProvider,
            IPublishEndpoint publisher)
        {
            _blocksReader = blocksReader;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _blockAssetsProvider = blockAssetsProvider;
            _publisher = publisher;
        }

        public async Task<IOngoingBlockIndexingStrategy> StartBlockIndexing(long blockNumber)
        {
            var block = await _blocksReader.ReadNonceBlockOrDefault(blockNumber);

            return new NonceOngoingBlockIndexingStrategy(
                block, 
                _blockchainDbUnitOfWorkFactory,
                _blockAssetsProvider,
                _publisher);
        }
    }
}
