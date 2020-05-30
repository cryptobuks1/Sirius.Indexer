using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain
{
    public interface IBlockHeadersRepository
    {
        Task InsertOrIgnore(BlockHeader blockHeader);
        Task<BlockHeader> GetOrDefault(string blockchainId, long blockNumber);
        Task Remove(string globalId);
        Task<IEnumerable<BlockHeader>> GetBatch(string blockchainId, long startBlockNumber, int limit);
    }
}
