using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Persistence.Entities.BlockHeaders;

namespace IndexerTests.Sdk.Mocks.Persistence
{
    public class InMemoryBlockHeadersRepository : IBlockHeadersRepository
    {
        private readonly Dictionary<string, BlockHeader> _store = new Dictionary<string, BlockHeader>();

        public Task InsertOrIgnore(BlockHeader blockHeader)
        {
            lock (_store)
            {
                if (!_store.ContainsKey(blockHeader.Id))
                {
                    _store[blockHeader.Id] = blockHeader;
                }
            }

            return Task.CompletedTask;
        }

        public Task<BlockHeader> GetOrDefault(long blockNumber)
        {
            lock (_store)
            {
                var block = _store.Values.SingleOrDefault(x => x.Number == blockNumber);

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

        public Task<IEnumerable<BlockHeader>> GetBatch(long startBlockNumber, int limit)
        {
            lock (_store)
            {
                var blocks = _store.Values
                    .OrderBy(x => x.Number)
                    .SkipWhile(x => x.Number < startBlockNumber)
                    .Take(limit)
                    .ToArray();

                return Task.FromResult<IEnumerable<BlockHeader>>(blocks);
            }
        }

        public Task<BlockHeader> GetLast()
        {
            lock (_store)
            {
                return Task.FromResult(_store.Values.OrderByDescending(x => x.Number).First());
            }
        }
    }
}
