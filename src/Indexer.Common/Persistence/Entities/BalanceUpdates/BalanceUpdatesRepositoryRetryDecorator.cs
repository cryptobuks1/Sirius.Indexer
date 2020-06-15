using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Persistence.Entities.BalanceUpdates
{
    internal sealed class BalanceUpdatesRepositoryRetryDecorator : IBalanceUpdatesRepository
    {
        private readonly IBalanceUpdatesRepository _impl;

        public BalanceUpdatesRepositoryRetryDecorator(IBalanceUpdatesRepository impl)
        {
            _impl = impl;
        }

        public Task InsertOrIgnore(string blockchainId, IReadOnlyCollection<BalanceUpdate> balanceUpdates)
        {
            return _impl.InsertOrIgnore(blockchainId, balanceUpdates);
        }
    }
}
