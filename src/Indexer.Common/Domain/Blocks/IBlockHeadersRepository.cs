using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Blocks
{
    public interface IBlockHeadersRepository
    {
        Task InsertOrIgnore(BlockHeader blockHeader);
        Task<BlockHeader> GetOrDefault(string blockchainId, long blockNumber);
        Task Remove(string blockchainId, string id);
        Task<IEnumerable<BlockHeader>> GetBatch(string blockchainId, long startBlockNumber, int limit);
    }
}
