using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    internal sealed class NonceOngoingIndexingStrategy : IOngoingIndexingStrategy
    {
        public Task<IOngoingBlockIndexingStrategy> StartBlockIndexing(long blockNumber)
        {
            throw new System.NotImplementedException();
        }
    }
}
