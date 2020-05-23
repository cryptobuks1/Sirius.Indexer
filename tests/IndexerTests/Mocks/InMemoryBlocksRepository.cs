using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain;

namespace IndexerTests.Mocks
{
    public class InMemoryBlocksRepository : IBlocksRepository
    {
        private readonly Dictionary<string, Block> _store = new Dictionary<string, Block>();

        public Task InsertOrIgnore(Block block)
        {
            lock (_store)
            {
                if (!_store.ContainsKey(block.GlobalId))
                {
                    _store[block.GlobalId] = block;
                }
            }

            return Task.CompletedTask;
        }

        public Task<Block> GetOrDefault(string blockchainId, long blockNumber)
        {
            lock (_store)
            {
                var block = _store.Values.SingleOrDefault(x => x.BlockchainId == blockchainId && x.Number == blockNumber);

                return Task.FromResult(block);
            }
        }

        public Task Remove(string globalId)
        {
            lock (_store)
            {
                _store.Remove(globalId);
            }

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Block>> GetBatch(string blockchainId, long startBlockNumber, int limit)
        {
            lock (_store)
            {
                var blocks = _store.Values
                    .Where(x => x.BlockchainId == blockchainId)
                    .OrderBy(x => x.Number)
                    .SkipWhile(x => x.Number < startBlockNumber)
                    .Take(limit)
                    .ToArray();

                return Task.FromResult<IEnumerable<Block>>(blocks);
            }
        }
    }
}
