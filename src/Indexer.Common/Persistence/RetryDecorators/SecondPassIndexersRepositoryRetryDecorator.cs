using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.RetryDecorators
{
    internal class SecondPassIndexersRepositoryRetryDecorator : ISecondPassIndexersRepository
    {
        private readonly ISecondPassIndexersRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public SecondPassIndexersRepositoryRetryDecorator(ISecondPassIndexersRepository impl)
        {
            _impl = impl;
            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }

        public Task<SecondPassIndexer> Get(string blockchainId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Get(blockchainId));
        }

        public Task<SecondPassIndexer> GetOrDefault(string blockchainId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetOrDefault(blockchainId));
        }

        public Task Add(SecondPassIndexer indexer)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Add(indexer));
        }

        public Task<SecondPassIndexer> Update(SecondPassIndexer indexer)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Update(indexer));
        }
    }
}