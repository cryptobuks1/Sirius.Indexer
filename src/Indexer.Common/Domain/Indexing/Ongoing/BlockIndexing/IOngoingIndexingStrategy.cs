using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    public interface IOngoingIndexingStrategy
    {
        Task<IOngoingBlockIndexingStrategy> StartBlockIndexing(long blockNumber);
    }
}
