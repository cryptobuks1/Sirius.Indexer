using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.RetryDecorators
{
    internal class OngoingIndexersRepositoryRetryDecorator : IOngoingIndexersRepository
    {
        private readonly IOngoingIndexersRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public OngoingIndexersRepositoryRetryDecorator(IOngoingIndexersRepository impl)
        {
            _impl = impl;
            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }

        public Task<OngoingIndexer> Get(string blockchainId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Get(blockchainId));
        }

        public Task<OngoingIndexer> Update(OngoingIndexer indexer)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Update(indexer));
        }

        public Task<OngoingIndexer> GetOrDefault(string blockchainId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetOrDefault(blockchainId));
        }

        public Task Add(OngoingIndexer indexer)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Add(indexer));
        }

        public Task Remove(string blockchainId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Remove(blockchainId));
        }
    }
}
