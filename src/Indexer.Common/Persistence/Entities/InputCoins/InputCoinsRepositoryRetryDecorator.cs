using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.Entities.InputCoins
{
    internal sealed class InputCoinsRepositoryRetryDecorator : IInputCoinsRepository
    {
        private readonly IInputCoinsRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public InputCoinsRepositoryRetryDecorator(IInputCoinsRepository impl)
        {
            _impl = impl;

            _retryPolicy = RetryPolicies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<InputCoin> coins)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(blockchainId, blockId, coins));
        }

        public Task<IReadOnlyCollection<InputCoin>> GetByBlock(string blockchainId, string blockId)
        {
            return _impl.GetByBlock(blockchainId, blockId);
        }

        public Task RemoveByBlock(string blockchainId, string blockId)
        {
            return _impl.RemoveByBlock(blockchainId, blockId);
        }
    }
}
