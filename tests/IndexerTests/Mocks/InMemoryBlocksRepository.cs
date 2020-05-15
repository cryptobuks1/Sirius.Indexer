using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain;

namespace IndexerTests.Mocks
{
    public class InMemoryBlocksRepository : IBlocksRepository
    {
        private readonly Dictionary<string, Block> _store = new Dictionary<string, Block>();

        public Task InsertOrReplace(Block block)
        {
            lock (_store)
            {
                _store[block.GlobalId] = block;
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

        public Task Remove(string id)
        {
            lock (_store)
            {
                _store.Remove(id);
            }

            return Task.CompletedTask;
        }
    }
}
