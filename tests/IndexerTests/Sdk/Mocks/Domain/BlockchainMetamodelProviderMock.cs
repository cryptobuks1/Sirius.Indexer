using System.Threading.Tasks;
using Indexer.Common.Domain.Blockchains;
using Indexer.Common.ReadModel.Blockchains;

namespace IndexerTests.Sdk.Mocks.Domain
{
    public class BlockchainMetamodelProviderMock : IBlockchainMetamodelProvider
    {
        public BlockchainMetamodel Metamodel { get; set; }

        public Task<BlockchainMetamodel> Get(string blockchainId)
        {
            return Task.FromResult(Metamodel);
        }
    }
}
