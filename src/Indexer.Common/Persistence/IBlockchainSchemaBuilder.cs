using System.Threading.Tasks;

namespace Indexer.Common.Persistence
{
    public interface IBlockchainSchemaBuilder
    {
        Task<bool> ProvisionForIndexing(string blockchainId);
        Task ProceedToOngoingIndexing(string blockchainId);
    }
}
