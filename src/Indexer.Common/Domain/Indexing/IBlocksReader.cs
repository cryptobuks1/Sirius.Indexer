using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing
{
    public interface IBlocksReader
    {
        Task<CoinsBlock> ReadCoinsBlockOrDefault(long blockNumber);
        Task<NonceBlock> ReadNonceBlockOrDefault(long blockNumber);
    }
}
