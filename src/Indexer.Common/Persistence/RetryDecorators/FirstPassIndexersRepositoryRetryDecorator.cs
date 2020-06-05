using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Indexing;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.RetryDecorators
{
    internal class FirstPassIndexersRepositoryRetryDecorator : IFirstPassIndexersRepository
    {
        private readonly IFirstPassIndexersRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public FirstPassIndexersRepositoryRetryDecorator(IFirstPassIndexersRepository impl)
        {
            _impl = impl;
            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }

        public Task<FirstPassIndexer> Get(FirstPassIndexerId id)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Get(id));
        }

        public Task<FirstPassIndexer> GetOrDefault(FirstPassIndexerId id)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetOrDefault(id));
        }

        public Task Add(FirstPassIndexer indexer)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Add(indexer));
        }

        public Task<FirstPassIndexer> Update(FirstPassIndexer indexer)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Update(indexer));
        }

        public Task<IEnumerable<FirstPassIndexer>> GetByBlockchain(string blockchainId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetByBlockchain(blockchainId));
        }

        public Task Remove(string blockchainId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.Remove(blockchainId));
        }
    }
}
