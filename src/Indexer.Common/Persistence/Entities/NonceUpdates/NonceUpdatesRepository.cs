using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Npgsql;

namespace Indexer.Common.Persistence.Entities.NonceUpdates
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
