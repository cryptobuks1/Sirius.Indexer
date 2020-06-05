using System.Threading.Tasks;

namespace Indexer.Common.Domain.Blocks
{
    public interface IBlocksReader
    {
        Task<CoinsBlock> ReadCoinsBlockOrDefault(long blockNumber);
        Task<NonceBlock> ReadNonceBlockOrDefault(long blockNumber);
    }
}
