using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Common;
using Indexer.Common.Persistence;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    internal sealed class NonceOngoingBlockIndexingStrategy : IOngoingBlockIndexingStrategy
    {
        private readonly NonceBlock _block;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;
        private readonly NonceFeesFactory _feesFactory;
        private readonly NonceBalanceUpdatesCalculator _balanceUpdatesCalculator;

        public NonceOngoingBlockIndexingStrategy(NonceBlock block,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory,
            NonceFeesFactory feesFactory,
            NonceBalanceUpdatesCalculator balanceUpdatesCalculator)
        {
            _block = block;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
            _feesFactory = feesFactory;
            _balanceUpdatesCalculator = balanceUpdatesCalculator;
        }

        public bool IsBlockFound => _block != null;
        public BlockHeader BlockHeader => _block.Header;

        public async Task ApplyBlock(OngoingIndexer indexer)
        {
            await using var unitOfWork = await _blockchainDbUnitOfWorkFactory.Start(indexer.BlockchainId);

            var nonceUpdates = _block.Transfers.SelectMany(tx => tx.NonceUpdates).ToArray();

            // TODO: Save operations

            await unitOfWork.NonceUpdates.InsertOrIgnore(nonceUpdates);

            var fees = await _feesFactory.Create(_block.Transfers);

            await unitOfWork.Fees.InsertOrIgnore(fees);

            var balanceUpdates = await _balanceUpdatesCalculator.Calculate(_block);

            await unitOfWork.BalanceUpdates.InsertOrIgnore(balanceUpdates);
            await unitOfWork.TransactionHeaders.InsertOrIgnore(_block.Transfers.Select(x => x.Header).ToArray());
            await unitOfWork.BlockHeaders.InsertOrIgnore(_block.Header);
        }
    }
}
