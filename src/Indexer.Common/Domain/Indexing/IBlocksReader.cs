using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IBlocksReader
    {
        Task<Block> ReadBlockOrDefaultAsync(long blockNumber);
    }
}
