using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Durability;
using Polly.Retry;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.InputCoins
{
    internal sealed class InputCoinsRepositoryRetryDecorator : IInputCoinsRepository
    {
        private readonly IInputCoinsRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public InputCoinsRepositoryRetryDecorator(IInputCoinsRepository impl)
        {
            _impl = impl;

            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<CoinId> coins)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(blockchainId, blockId, coins));
        }
    }
}
