using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    public interface IOngoingBlockIndexingStrategy
    {
        bool IsBlockFound { get; }
        BlockHeader BlockHeader { get; }
        int TransfersCount { get; }

        Task ApplyBlock(OngoingIndexer indexer);
    }
}
