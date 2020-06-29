using System.Threading.Tasks;
using Npgsql;

namespace Indexer.Common.Persistence
{
    public interface IBlockchainDbConnectionFactory
    {
        Task<NpgsqlConnection> Create(string blockchainId);
    }
}
