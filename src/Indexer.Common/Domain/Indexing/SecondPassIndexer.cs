using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indexer.Common.Domain.Indexing
{
    public sealed class SecondPassIndexer
    {
        private readonly List<object> _events = new List<object>();

        private SecondPassIndexer(string blockchainId,
            long nextBlock,
            long sequence,
            long version)
        {
            BlockchainId = blockchainId;
            NextBlock = nextBlock;
            Sequence = sequence;
            Version = version;
        }

        public string BlockchainId { get; }
        public long NextBlock { get; }
        public long Sequence { get; }
        public long Version { get; }
        public IReadOnlyCollection<object> Events => _events;

        public async Task IndexAvailableBlocks(int maxBlocksCount)
        {

        }
    }
}
