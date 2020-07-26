using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Persistence.Entities.BlockHeaders
{
    public interface IBlockHeadersRepository
    {
        Task InsertOrIgnore(BlockHeader blockHeader);
        Task<BlockHeader> GetOrDefault(long blockNumber);
        Task Remove(string id);
        Task<IEnumerable<BlockHeader>> GetBatch(long startBlockNumber, int limit);
        Task<BlockHeader> GetLast();
        Task<long> GetCount();
    }
}
