using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Coins;
using Indexer.Common.Durability;
using Polly.Retry;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.UnspentCoins
{
    internal sealed class UnspentCoinsRepositoryRetryDecorator : IUnspentCoinsRepository
    {
        private readonly IUnspentCoinsRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public UnspentCoinsRepositoryRetryDecorator(IUnspentCoinsRepository impl)
        {
            _impl = impl;
            _retryPolicy = RetryPolicies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(IReadOnlyCollection<UnspentCoin> coins)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(coins));
        }

        public Task<IReadOnlyCollection<UnspentCoin>> GetAnyOf(IReadOnlyCollection<CoinId> ids)
        {
            return _impl.GetAnyOf(ids);
        }

        public Task Remove(IReadOnlyCollection<CoinId> ids)
        {
            return _impl.Remove(ids);
        }

        public Task<IReadOnlyCollection<UnspentCoin>> GetByBlock(string blockId)
        {
            return _impl.GetByBlock(blockId);
        }

        public Task<IReadOnlyCollection<UnspentCoin>> GetByAddress(string address, long? asAtBlockNumber)
        {
            return _impl.GetByAddress(address, asAtBlockNumber);
        }

        public Task RemoveByBlock(string blockId)
        {
            return _impl.RemoveByBlock(blockId);
        }
    }
}
