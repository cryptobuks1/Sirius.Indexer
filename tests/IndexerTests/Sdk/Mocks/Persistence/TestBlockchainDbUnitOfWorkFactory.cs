using System.Threading.Tasks;
using Indexer.Common.Persistence;

namespace IndexerTests.Sdk.Mocks.Persistence
{
    public class TestBlockchainDbUnitOfWorkFactory : IBlockchainDbUnitOfWorkFactory
    {
        public IBlockchainDbUnitOfWork UnitOfWork { get; set; } = new TestBlockchainDbUnitOfWork();
        public ITransactionalBlockchainDbUnitOfWork TransactionalUnitOfWork { get; set; } = new TestTransactionalBlockchainDbUnitOfWork();
        
        public Task<IBlockchainDbUnitOfWork> Start(string blockchainId)
        {
            return Task.FromResult(UnitOfWork);
        }

        public Task<ITransactionalBlockchainDbUnitOfWork> StartTransactional(string blockchainId)
        {
            return Task.FromResult(TransactionalUnitOfWork);
        }
    }
}
