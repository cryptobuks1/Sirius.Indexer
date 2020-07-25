using System.Threading.Tasks;
using Indexer.Common.Persistence;

namespace IndexerTests.Sdk.Mocks.Persistence
{
    public class TestTransactionalBlockchainDbUnitOfWork : TestBlockchainDbUnitOfWork, ITransactionalBlockchainDbUnitOfWork
    {

        public Task Commit()
        {
            return Task.CompletedTask;
        }

        public Task Rollback()
        {
            return Task.CompletedTask;
        }
    }
}
