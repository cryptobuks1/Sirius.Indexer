using System;
using System.Threading.Tasks;
using Indexer.Common.Persistence;
using Npgsql;

namespace IndexerTests.Sdk.Mocks
{
    public class TestBlockchainDbConnectionFactory : IBlockchainDbConnectionFactory
    {
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public TestBlockchainDbConnectionFactory(Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public Task<NpgsqlConnection> Create(string blockchainId)
        {
            return _connectionFactory.Invoke();
        }
    }
}
