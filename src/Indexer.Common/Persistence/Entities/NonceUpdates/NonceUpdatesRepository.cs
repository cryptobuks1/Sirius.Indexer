using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers.Nonce;
using Npgsql;
using PostgreSQLCopyHelper;

namespace Indexer.Common.Persistence.Entities.NonceUpdates
{
    internal sealed class NonceUpdatesRepository : INonceUpdatesRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;

        public NonceUpdatesRepository(NpgsqlConnection connection, string schema)
        {
            _connection = connection;
            _schema = schema;
        }

        public async Task InsertOrIgnore(IReadOnlyCollection<NonceUpdate> nonceUpdates)
        {
            if (!nonceUpdates.Any())
            {
                return;
            }

            var copyHelper = new PostgreSQLCopyHelper<NonceUpdate>(_schema, TableNames.NonceUpdates)
                .UsePostgresQuoting()
                .MapVarchar(nameof(NonceUpdateEntity.address), x => x.Address)
                .MapVarchar(nameof(NonceUpdateEntity.transaction_id), x => x.TransactionId)
                .MapBigInt(nameof(NonceUpdateEntity.nonce), x => (int) x.Nonce);

            try
            {
                await copyHelper.SaveAllAsync(_connection, nonceUpdates);
            }
            catch (PostgresException e) when (e.IsPrimaryKeyViolationException())
            {
                var notExisted = await ExcludeExistingInDb(nonceUpdates);

                if (notExisted.Any())
                {
                    await copyHelper.SaveAllAsync(_connection, notExisted);
                }
            }
        }

        private async Task<IReadOnlyCollection<NonceUpdate>> ExcludeExistingInDb(IReadOnlyCollection<NonceUpdate> nonceUpdates)
        {
            if (!nonceUpdates.Any())
            {
                return Array.Empty<NonceUpdate>();
            }
            
            var existingEntities = await _connection.QueryInList<NonceUpdateEntity, NonceUpdate>(
                _schema,
                TableNames.NonceUpdates,
                nonceUpdates,
                columnsToSelect: "address, transaction_id",
                listColumns: "address, transaction_id",
                x => $"'{x.Address}', '{x.TransactionId}'",
                knownSourceLength: nonceUpdates.Count);

            var existing = existingEntities
                .Select(x => (x.address, x.transaction_id))
                .ToHashSet();

            return nonceUpdates.Where(x => !existing.Contains((x.Address, x.TransactionId))).ToArray();
        }
    }
}
