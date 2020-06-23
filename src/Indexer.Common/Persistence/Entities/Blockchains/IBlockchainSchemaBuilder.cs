using System.Threading.Tasks;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.Blockchains
{
    public interface IBlockchainSchemaBuilder
    {
        Task<bool> ProvisionForIndexing(string blockchainId, DoubleSpendingProtectionType blockchainDoubleSpendingProtectionType);
        Task UpgradeToOngoingIndexing(string blockchainId, DoubleSpendingProtectionType blockchainDoubleSpendingProtectionType);
    }
}
