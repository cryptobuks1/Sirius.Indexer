using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Domain.Indexing.RetryDecorators
{
    public class BlocksReaderRetryDecorator : IBlocksReader
    {
        private readonly IBlocksReader _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public BlocksReaderRetryDecorator(IBlocksReader impl)
        {
            _impl = impl;
            _retryPolicy = Policies.DefaultWebServiceRetryPolicy();
        }

        public Task<CoinsBlock> ReadCoinsBlockOrDefault(long blockNumber)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.ReadCoinsBlockOrDefault(blockNumber));
        }

        public Task<NonceBlock> ReadNonceBlockOrDefault(long blockNumber)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.ReadNonceBlockOrDefault(blockNumber));
        }
    }
}
