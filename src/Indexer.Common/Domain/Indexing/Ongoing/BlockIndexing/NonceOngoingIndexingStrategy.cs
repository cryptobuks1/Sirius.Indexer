using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Persistence;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    internal sealed class NonceOngoingIndexingStrategy : IOngoingIndexingStrategy
    {
        private readonly IBlocksReader _blocksReader;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly NonceFeesFactory _feesFactory;
        private readonly NonceBalanceUpdatesCalculator _balanceUpdatesCalculator;

        public NonceOngoingIndexingStrategy(IBlocksReader blocksReader, 
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory, 
            NonceFeesFactory feesFactory,
            NonceBalanceUpdatesCalculator balanceUpdatesCalculator)
        {
            _blocksReader = blocksReader;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _feesFactory = feesFactory;
            _balanceUpdatesCalculator = balanceUpdatesCalculator;
        }

        public async Task<IOngoingBlockIndexingStrategy> StartBlockIndexing(long blockNumber)
        {
            var block = await _blocksReader.ReadNonceBlockOrDefault(blockNumber);

            return new NonceOngoingBlockIndexingStrategy(
                block, 
                _blockchainDbUnitOfWorkFactory,
                _feesFactory,
                _balanceUpdatesCalculator);
        }
    }
}
