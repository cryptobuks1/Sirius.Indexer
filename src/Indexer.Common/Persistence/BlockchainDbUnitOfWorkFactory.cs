using System.Threading.Tasks;

namespace Indexer.Common.Persistence
{
    public sealed class BlockchainDbUnitOfWorkFactory : IBlockchainDbUnitOfWorkFactory
    {
        private readonly IBlockchainDbConnectionFactory _connectionFactory;

        public BlockchainDbUnitOfWorkFactory(IBlockchainDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IBlockchainDbUnitOfWork> Start(string blockchainId)
        {
            var connection = await _connectionFactory.Create(blockchainId);
           
            return new BlockchainDbUnitOfWork(connection, blockchainId);
        }
    }
}
