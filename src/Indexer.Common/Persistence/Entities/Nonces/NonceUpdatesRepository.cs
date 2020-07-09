using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Nonces;
using Npgsql;

namespace Indexer.Common.Persistence.Entities.Nonces
{
    internal sealed class NonceUpdatesRepository : INonceUpdatesRepository
    {
        public NonceUpdatesRepository(NpgsqlConnection connection, string schema)
        {
            throw new System.NotImplementedException();
        }

        public Task InsertOrIgnore(IReadOnlyCollection<NonceUpdate> nonceUpdates)
        {
            throw new System.NotImplementedException();
        }
    }
}
