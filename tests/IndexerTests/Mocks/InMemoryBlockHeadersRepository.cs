using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain;
using Indexer.Common.Domain.Blocks;

namespace IndexerTests.Mocks
{
    public class InMemoryBlockHeadersRepository : IBlockHeadersRepository
    {
        private readonly Dictionary<string, BlockHeader> _store = new Dictionary<string, BlockHeader>();

        public Task InsertOrIgnore(BlockHeader blockHeader)
        {
            lock (_store)
            {
                if (!_store.ContainsKey(blockHeader.GlobalId))
                {
                    _store[blockHeader.GlobalId] = blockHeader;
                }
            }

            return Task.CompletedTask;
        }

        public Task<BlockHeader> GetOrDefault(string blockchainId, long blockNumber)
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

        public Task<IEnumerable<BlockHeader>> GetBatch(string blockchainId, long startBlockNumber, int limit)
        {
            lock (_store)
            {
                var blocks = _store.Values
                    .Where(x => x.BlockchainId == blockchainId)
                    .OrderBy(x => x.Number)
                    .SkipWhile(x => x.Number < startBlockNumber)
                    .Take(limit)
                    .ToArray();

                return Task.FromResult<IEnumerable<BlockHeader>>(blocks);
            }
        }
    }
}
