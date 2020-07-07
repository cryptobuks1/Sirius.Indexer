using System.Threading.Tasks;
using Indexer.Common.ReadModel.Blockchains;

namespace Indexer.Common.Domain.Blockchains
{
    public interface IBlockchainMetamodelProvider
    {
        Task<BlockchainMetamodel> Get(string blockchainId);
    }
}
