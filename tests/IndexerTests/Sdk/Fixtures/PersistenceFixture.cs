using System.Collections.Concurrent;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.Blockchains;
using IndexerTests.Sdk.Containers.Postgres;
using IndexerTests.Sdk.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Swisschain.Sirius.Sdk.Primitives;
using Xunit;

namespace IndexerTests.Sdk.Fixtures
{
    public class PersistenceFixture : IAsyncLifetime
    {
        private readonly PostgresContainer _container;
        private readonly ConcurrentBag<NpgsqlConnection> _connections;
        private readonly BlockchainSchemaBuilder _schemaBuilder;

        public PersistenceFixture()
        {
            _container = new PostgresContainer("tests-pg", PortManager.GetNextPort());
            _connections = new ConcurrentBag<NpgsqlConnection>();
            _schemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, CreateConnection);

            BlockchainDbConnectionFactory = new TestBlockchainDbConnectionFactory(CreateConnection);
        }

        public string ConnectionString => _container.ConnectionString;
        public IBlockchainDbConnectionFactory BlockchainDbConnectionFactory { get; }

        public async Task<NpgsqlConnection> CreateConnection()
        {
            var connection = new NpgsqlConnection(_container.ConnectionString);

            await connection.OpenAsync();

            _connections.Add(connection);

            return connection;
        }

        public async Task CreateSchema(string blockchainName, DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            await _schemaBuilder.ProvisionForIndexing(blockchainName, doubleSpendingProtectionType);
        }

        public async Task InitializeAsync()
        {
            await _container.Start();
        }

        public async Task DisposeAsync()
        {
            foreach (var connection in _connections)
            {
                await connection.DisposeAsync();
            }

            _container.Stop();
        }
    }
}
