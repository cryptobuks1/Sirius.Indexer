using System.Threading.Tasks;
using Indexer.Common.Persistence;

namespace IndexerTests.Sdk.Mocks.Persistence
{
    public class TestBlockchainDbUnitOfWorkFactory : IBlockchainDbUnitOfWorkFactory
    {
        public IBlockchainDbUnitOfWork UnitOfWork { get; } = new TestBlockchainDbUnitOfWork();
        
        public Task<IBlockchainDbUnitOfWork> Start(string blockchainId)
        {
            return Task.FromResult(UnitOfWork);
        }
    }
}
