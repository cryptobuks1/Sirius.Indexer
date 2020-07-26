using System.Collections.Concurrent;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.Blockchains;
using IndexerTests.Sdk.Containers.Postgres;
using IndexerTests.Sdk.Mocks.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace IndexerTests.Sdk.Fixtures
{
    public class PersistenceFixture : IAsyncLifetime
    {
        private readonly PostgresContainer _container;
        private readonly ConcurrentBag<NpgsqlConnection> _testDbConnections;

        public PersistenceFixture()
        {
            _container = new PostgresContainer("tests-pg", PortManager.GetNextPort());
            _testDbConnections = new ConcurrentBag<NpgsqlConnection>();
        }

        public IBlockchainDbConnectionFactory BlockchainDbConnectionFactory { get; private set; }
        public IBlockchainDbUnitOfWorkFactory BlockchainDbUnitOfWorkFactory { get; private set; }
        public IBlockchainSchemaBuilder SchemaBuilder { get; private set; }

        public async Task<NpgsqlConnection> CreateConnection()
        {
            var connection = new NpgsqlConnection(_container.GetConnectionString("test_db"));

            await connection.OpenAsync();

            return connection;
        }

        public async Task CreateTestDb()
        {
            await using var connection = new NpgsqlConnection(_container.MainDbConnectionString);
            await connection.ExecuteAsync("create database test_db");

            BlockchainDbConnectionFactory = new TestBlockchainDbConnectionFactory(CreateConnectionInternal);
            BlockchainDbUnitOfWorkFactory = new BlockchainDbUnitOfWorkFactory(BlockchainDbConnectionFactory);
            SchemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, BlockchainDbConnectionFactory);
        }

        public async Task DropTestDb()
        {
            foreach (var testDbConnection in _testDbConnections)
            {
                await testDbConnection.CloseAsync();
                await testDbConnection.DisposeAsync();
            }

            _testDbConnections.Clear();

            await using var connection = new NpgsqlConnection(_container.MainDbConnectionString);

            var query = @"
                -- Disallow new connections
                update pg_database set datallowconn = 'false' where datname = 'test_db';
                alter database test_db connection limit 1;

                -- Terminate existing connections
                select pg_terminate_backend(pid) from pg_stat_activity where datname = 'test_db';

                -- Drop database
                drop database test_db";

            await connection.ExecuteAsync(query);
        }

        public async Task InitializeAsync()
        {
            await _container.Start();
        }

        public async Task DisposeAsync()
        {
            foreach (var connection in _testDbConnections)
            {
                await connection.DisposeAsync();
            }

            _container.Stop();
        }

        private async Task<NpgsqlConnection> CreateConnectionInternal()
        {
            var connection = await CreateConnection();

            _testDbConnections.Add(connection);

            return connection;
        }
    }
}
