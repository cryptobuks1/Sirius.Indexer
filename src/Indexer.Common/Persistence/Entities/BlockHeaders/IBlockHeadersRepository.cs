using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Persistence.Entities.BlockHeaders
{
    public interface IBlockHeadersRepository
    {
        Task InsertOrIgnore(BlockHeader blockHeader);
        Task<BlockHeader> GetOrDefault(string blockchainId, long blockNumber);
        Task Remove(string blockchainId, string id);
        Task<IEnumerable<BlockHeader>> GetBatch(string blockchainId, long startBlockNumber, int limit);
        Task<BlockHeader> GetLast(string blockchainId);
    }
}
