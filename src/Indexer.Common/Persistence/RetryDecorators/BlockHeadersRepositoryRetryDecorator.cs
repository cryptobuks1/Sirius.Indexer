using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.RetryDecorators
{
    internal class BlockHeadersRepositoryRetryDecorator : IBlockHeadersRepository
    {
        private readonly IBlockHeadersRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public BlockHeadersRepositoryRetryDecorator(IBlockHeadersRepository impl)
        {
            _impl = impl;

            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(BlockHeader blockHeader)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(blockHeader));
        }

        public Task<BlockHeader> GetOrDefault(string blockchainId, long blockNumber)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetOrDefault(blockchainId, blockNumber));
        }

        public Task Remove(string globalId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Remove(globalId));
        }

        public Task<IEnumerable<BlockHeader>> GetBatch(string blockchainId, long startBlockNumber, int limit)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetBatch(blockchainId, startBlockNumber, limit));
        }
    }
}
