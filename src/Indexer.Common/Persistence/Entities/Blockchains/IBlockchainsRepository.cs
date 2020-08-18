using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.ReadModel.Blockchains;

namespace Indexer.Common.Persistence.Entities.Blockchains
{
    public interface IBlockchainsRepository
    {
        Task<IReadOnlyCollection<BlockchainMetamodel>> GetAllAsync(string cursor, int limit);
        Task Upsert(BlockchainMetamodel blockchainMetamodel);
        Task<BlockchainMetamodel> GetAsync(string blockchainId);
        Task<BlockchainMetamodel> GetOrDefaultAsync(string blockchainId);
    }
}
