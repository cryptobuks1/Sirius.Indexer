using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Nonces;

namespace Indexer.Common.Persistence.Entities.Nonces
{
    public interface INonceUpdatesRepository
    {
        Task InsertOrIgnore(IReadOnlyCollection<NonceUpdate> nonceUpdates);
    }
}
