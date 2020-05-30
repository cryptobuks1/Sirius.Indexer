using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IBlocksReader
    {
        Task<BlockHeader> ReadBlockOrDefaultAsync(long blockNumber);
    }
}
