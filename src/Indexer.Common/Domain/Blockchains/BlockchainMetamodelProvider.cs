using System.Collections.Concurrent;
using System.Threading.Tasks;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.ReadModel.Blockchains;

namespace Indexer.Common.Domain.Blockchains
{
    internal sealed class BlockchainMetamodelProvider : IBlockchainMetamodelProvider
    {
        private readonly IBlockchainsRepository _blockchainsRepository;
        private readonly ConcurrentDictionary<string, BlockchainMetamodel> _blockchainMetamodelCache;

        public BlockchainMetamodelProvider(IBlockchainsRepository blockchainsRepository)
        {
            _blockchainsRepository = blockchainsRepository;
            _blockchainMetamodelCache = new ConcurrentDictionary<string, BlockchainMetamodel>();
        }

        public async Task<BlockchainMetamodel> Get(string blockchainId)
        {
            if (_blockchainMetamodelCache.ContainsKey(blockchainId))
            {
                return _blockchainMetamodelCache[blockchainId];
            }

            var blockchainMetamodel = await _blockchainsRepository.GetAsync(blockchainId);

            if (!_blockchainMetamodelCache.TryAdd(blockchainId, blockchainMetamodel))
            {
                return _blockchainMetamodelCache[blockchainId];
            }

            return blockchainMetamodel;
        }
    }
}
