using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Coins;
using Indexer.Common.Durability;
using Polly.Retry;

namespace Indexer.Common.Persistence.Entities.SpentCoins
{
    internal sealed class SpentCoinsRepositoryRetryDecorator : ISpentCoinsRepository
    {
        private readonly ISpentCoinsRepository _impl;
        private readonly AsyncRetryPolicy _retryPolicy;

        public SpentCoinsRepositoryRetryDecorator(ISpentCoinsRepository impl)
        {
            _impl = impl;
            _retryPolicy = RetryPolicies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(IReadOnlyCollection<SpentCoin> coins)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(coins));
        }

        public Task<IReadOnlyCollection<SpentCoin>> GetSpentByBlock(string blockId)
        {
            return _impl.GetSpentByBlock(blockId);
        }

        public Task RemoveSpentByBlock(string blockId)
        {
            return _impl.RemoveSpentByBlock(blockId);
        }
    }
}
