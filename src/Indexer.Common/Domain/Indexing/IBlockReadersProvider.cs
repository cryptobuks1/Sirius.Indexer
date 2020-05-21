using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IBlockReadersProvider
    {
        Task<IBlocksReader> Get(string blockchainId);
    }
}