using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling
{
    public interface IBlockCanceler
    {
        Task Cancel(OngoingIndexer indexer, BlockHeader blockHeader);
    }
}
