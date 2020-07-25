using System.Threading.Tasks;

namespace Indexer.Common.Persistence.BlockchainDbMigrations
{
    public interface IBlockchainDbMigrationsManager
    {
        Task Migrate(string blockchainId);
        Task Validate(string blockchainId);
    }
}
