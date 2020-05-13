using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.ReadModel.Blockchains;

namespace Indexer.Common.Persistence
{
    public interface IBlockchainsRepository
    {
        Task<IReadOnlyCollection<BlockchainMetamodel>> GetAllAsync(string cursor, int limit);
        Task AddOrReplaceAsync(BlockchainMetamodel blockchainMetamodel);
        Task<BlockchainMetamodel> GetAsync(string blockchainId);
    }
}
