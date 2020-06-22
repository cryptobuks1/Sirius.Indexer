using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.ObservedOperations;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.Entities.ObservedOperations
{
    internal sealed class ObservedOperationsRepositoryRetryDecorator : IObservedOperationsRepository
    {
        private readonly IObservedOperationsRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public ObservedOperationsRepositoryRetryDecorator(IObservedOperationsRepository impl)
        {
            _impl = impl;
            _retryPolicy = RetryPolicies.DefaultRepositoryRetryPolicy();
        }

        public Task AddOrIgnore(ObservedOperation observedOperation)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.AddOrIgnore(observedOperation));
        }

        public Task<IReadOnlyCollection<ObservedOperation>> GetInvolvedInBlock(string blockchainId, string blockId)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.GetInvolvedInBlock(blockchainId, blockId));
        }
    }
}
