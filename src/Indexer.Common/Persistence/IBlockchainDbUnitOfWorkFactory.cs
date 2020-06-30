using System.Threading.Tasks;

namespace Indexer.Common.Persistence
{
    public interface IBlockchainDbUnitOfWorkFactory
    {
        Task<IBlockchainDbUnitOfWork> Start(string blockchainId);
        Task<ITransactionalBlockchainDbUnitOfWork> StartTransactional(string blockchainId);
    }
}
