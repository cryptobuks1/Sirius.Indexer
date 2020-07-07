using System;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;

namespace Indexer.Common.Domain.Indexing.Ongoing.BlockIndexing
{
    internal sealed class NonceOngoingBlockIndexingStrategy : IOngoingBlockIndexingStrategy
    {
        private readonly NonceBlock _block;

        public NonceOngoingBlockIndexingStrategy(NonceBlock block)
        {
            _block = block;
        }

        public bool IsBlockFound => _block != null;
        public BlockHeader BlockHeader => _block.Header;

        public Task ApplyBlock(OngoingIndexer indexer)
        {
            throw new NotImplementedException();
        }
    }
}
