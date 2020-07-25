using System.Collections.Concurrent;
using System.Threading.Tasks;
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
        private readonly ConcurrentBag<NpgsqlConnection> _connections;

        public PersistenceFixture()
        {
            _container = new PostgresContainer("tests-pg", PortManager.GetNextPort());
            _connections = new ConcurrentBag<NpgsqlConnection>();

            BlockchainDbConnectionFactory = new TestBlockchainDbConnectionFactory(CreateConnection);
            BlockchainDbUnitOfWorkFactory = new BlockchainDbUnitOfWorkFactory(BlockchainDbConnectionFactory);
            SchemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, BlockchainDbConnectionFactory);
        }

        public string ConnectionString => _container.ConnectionString;
        public IBlockchainDbConnectionFactory BlockchainDbConnectionFactory { get; }
        public IBlockchainDbUnitOfWorkFactory BlockchainDbUnitOfWorkFactory { get; }
        public IBlockchainSchemaBuilder SchemaBuilder { get; }

        public async Task<NpgsqlConnection> CreateConnection()
        {
            var connection = new NpgsqlConnection(_container.ConnectionString);

            await connection.OpenAsync();

            _connections.Add(connection);

            return connection;
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
