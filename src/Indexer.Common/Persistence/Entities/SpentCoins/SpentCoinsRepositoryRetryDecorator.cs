using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;
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
            _retryPolicy = Policies.DefaultRepositoryRetryPolicy();
        }

        public Task InsertOrIgnore(string blockchainId, string blockId, IReadOnlyCollection<SpentCoin> coins)
        {
            return _retryPolicy.ExecuteAsync(() => _impl.InsertOrIgnore(blockchainId, blockId, coins));
        }
    }
}
