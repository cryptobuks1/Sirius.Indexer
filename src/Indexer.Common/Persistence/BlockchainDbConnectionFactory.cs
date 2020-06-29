using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Configuration;
using Npgsql;

namespace Indexer.Common.Persistence
{
    internal class BlockchainDbConnectionFactory : IBlockchainDbConnectionFactory
    {
        private readonly Dictionary<string, string> _blockchainConnectionStrings;

        public BlockchainDbConnectionFactory(AppConfig config)
        {
            _blockchainConnectionStrings = config.Blockchains.ToDictionary(x => x.Key, x => x.Value.Db.ConnectionString);
        }

        public async Task<NpgsqlConnection> Create(string blockchainId)
        {
            var connectionString = GetConnectionString(blockchainId);
            var connection = new NpgsqlConnection(connectionString);

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            return connection;
        }

        private string GetConnectionString(string blockchainId)
        {
            if (!_blockchainConnectionStrings.TryGetValue(blockchainId, out var connectionString))
            {
                throw new InvalidOperationException($"DB connection string for the blockchain {blockchainId} not found");
            }

            return connectionString;
        }
    }
}
