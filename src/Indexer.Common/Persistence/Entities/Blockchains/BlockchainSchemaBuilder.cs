using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Indexer.Common.Persistence.Entities.Blockchains
{
    internal sealed class BlockchainSchemaBuilder : IBlockchainSchemaBuilder
    {
        private readonly ILogger<BlockchainSchemaBuilder> _logger;
        private readonly Func<Task<NpgsqlConnection>> _connectionFactory;

        public BlockchainSchemaBuilder(ILogger<BlockchainSchemaBuilder> logger,
            Func<Task<NpgsqlConnection>> connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> ProvisionForIndexing(string blockchainId)
        {
            _logger.LogInformation("Provisioning DB schema for {@blockchainId}...", blockchainId);

            await using var connection = await _connectionFactory.Invoke();

            if (await CheckSchema(blockchainId, connection))
            {
                _logger.LogInformation("DB schema for {@blockchainId} already provisioned", blockchainId);

                return false;
            }

            await CreateSchema(blockchainId, connection);

            return true;
        }

        public async Task ProceedToOngoingIndexing(string blockchainId)
        {
            _logger.LogInformation("Proceeding DB schema for {@blockchainId} to ongoing indexing...", blockchainId);

            await using var connection = await _connectionFactory.Invoke();

            var query = await LoadScript("before-ongoing-indexing.sql");

            query = query.Replace("@schemaName", DbSchema.GetName(blockchainId));

            await connection.ExecuteAsync(query);

            _logger.LogInformation("DB schema for {@blockchainId} has been proceeded to ongoing indexing", blockchainId);
        }

        private async Task CreateSchema(string blockchainId, NpgsqlConnection connection)
        {
            _logger.LogInformation("Creating schema for {@blockchainId}...", blockchainId);
            
            var query = await LoadScript("before-indexing.sql");

            query = query.Replace("@schemaName", DbSchema.GetName(blockchainId));

            await connection.ExecuteAsync(query);

            _logger.LogInformation("DB schema for {@blockchainId} has been created", blockchainId);
        }

        private static async Task<bool> CheckSchema(string blockchainId, NpgsqlConnection connection)
        {
            var query = "select exists (select * from pg_catalog.pg_namespace where nspname = @schemaName)";

            var isExists = await connection.ExecuteScalarAsync<bool>(query, new {schemaName = DbSchema.GetName(blockchainId)});

            return isExists;
        }

        private static async Task<string> LoadScript(string fileName)
        {
            var entryPointPath = Assembly.GetEntryAssembly()?.Location;

            if (entryPointPath != null)
            {
                var binPath = Path.GetDirectoryName(entryPointPath);

                return await File.ReadAllTextAsync($@"{binPath}/Persistence/SqlScripts/{fileName}");
            }

            return await File.ReadAllTextAsync(fileName);
        }
    }
}
