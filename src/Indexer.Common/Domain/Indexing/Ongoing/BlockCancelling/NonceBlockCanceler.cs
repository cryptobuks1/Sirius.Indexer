using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockCancelling
{
    internal sealed class NonceBlockCanceler : IBlockCanceler
    {
        public Task Cancel(OngoingIndexer indexer, BlockHeader blockHeader)
        {
            throw new NotImplementedException();
        }
    }
}
