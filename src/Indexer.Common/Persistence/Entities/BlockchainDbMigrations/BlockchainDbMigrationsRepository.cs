using System;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Persistence.BlockchainDbMigrations;
using Npgsql;

namespace Indexer.Common.Persistence.Entities.BlockchainDbMigrations
{
    internal sealed class BlockchainDbMigrationsRepository
    {
        private readonly NpgsqlConnection _connection;
        private readonly string _schema;

        public BlockchainDbMigrationsRepository(NpgsqlConnection connection, string schema)
        {
            _connection = connection;
            _schema = schema;
        }

        public async Task<int> GetMaxVersion()
        {
            var query = $"select max(version) from {_schema}.migrations";

            try
            {
                return await _connection.ExecuteScalarAsync<int>(query);
            }
            catch (PostgresException e) when (e.SqlState == "42P01")
            {
                return 0;
            }
        }

        public async Task Add(BlockchainDbMigration migration)
        {
            var query = $"insert into {_schema}.migrations (version, script, date) values (@version, @script, @date)";

            await _connection.ExecuteAsync(
                query,
                new
                {
                    version = migration.Version,
                    script = migration.ScriptPath,
                    date = DateTime.UtcNow
                });
        }
    }
}
