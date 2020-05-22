using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indexer.Common.Domain
{
    public interface IBlocksRepository
    {
        Task InsertOrReplace(Block block);
        Task<Block> GetOrDefault(string blockchainId, long blockNumber);
        Task Remove(string globalId);
        Task<IEnumerable<Block>> GetBatch(string blockchainId, long startBlockNumber, int limit);
    }
}
