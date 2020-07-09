using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;

namespace Indexer.Common.Persistence.Entities.NonceUpdates
{
    public interface INonceUpdatesRepository
    {
        Task InsertOrIgnore(IReadOnlyCollection<NonceUpdate> nonceUpdates);
        Task<NonceUpdate> GetLatestOrDefault(string address, long? asAtBlockNumber);
    }
}
