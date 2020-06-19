using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;

namespace Indexer.Common.Persistence.Entities.BalanceUpdates
{
    public interface IBalanceUpdatesRepository
    {
        /// <summary>
        /// Only one item for the (address, assetId, blockNumber) tuple should be in the list.
        /// Only balance updates from the same block should be in the list.
        /// It's not checked due to performance reason
        /// </summary>
        Task InsertOrIgnore(string blockchainId, IReadOnlyCollection<BalanceUpdate> balanceUpdates);
        Task RemoveByBlock(string blockchainId, string blockId);
    }
}
