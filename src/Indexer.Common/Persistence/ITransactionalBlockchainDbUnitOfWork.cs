using System.Threading.Tasks;

namespace Indexer.Common.Persistence
{
    public interface ITransactionalBlockchainDbUnitOfWork : IBlockchainDbUnitOfWork
    {
        Task Commit();
        Task Rollback();
    }
}
