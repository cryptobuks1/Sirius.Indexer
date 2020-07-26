using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.Entities.BlockHeaders
{
    internal class BlockHeadersRepositoryRetryDecorator : IBlockHeadersRepository
    {
        private readonly IBlockHeadersRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public BlockHeadersRepositoryRetryDecorator(IBlockHeadersRepository impl)
        {
            _impl = impl;

            _retryPolicy = RetryPolicies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(BlockHeader blockHeader)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(blockHeader));
        }

        public Task<BlockHeader> GetOrDefault(long blockNumber)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetOrDefault(blockNumber));
        }

        public Task Remove(string id)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Remove(id));
        }

        public Task<IEnumerable<BlockHeader>> GetBatch(long startBlockNumber, int limit)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetBatch(startBlockNumber, limit));
        }

        public Task<BlockHeader> GetLast()
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetLast());
        }

        public Task<long> GetCount()
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetCount());
        }
    }
}
