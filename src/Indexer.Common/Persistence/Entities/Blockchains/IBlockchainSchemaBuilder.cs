using System.Threading.Tasks;

namespace Indexer.Common.Persistence.Entities.Blockchains
{
    public interface IBlockchainSchemaBuilder
    {
        Task<bool> ProvisionForIndexing(string blockchainId);
        Task ProceedToOngoingIndexing(string blockchainId);
    }
}
