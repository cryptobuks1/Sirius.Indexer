using System.Threading.Tasks;

namespace Indexer.Common.Domain.Blocks
{
    public interface IBlockReadersProvider
    {
        Task<IBlocksReader> Get(string blockchainId);
    }
}