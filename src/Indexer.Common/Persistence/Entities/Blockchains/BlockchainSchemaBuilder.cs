using System;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Persistence.SqlScripts;
using Microsoft.Extensions.Logging;
using Npgsql;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.Entities.Blockchains
{
    internal sealed class BlockchainSchemaBuilder : IBlockchainSchemaBuilder
    {
        private readonly ILogger<BlockchainSchemaBuilder> _logger;
        private readonly IBlockchainDbConnectionFactory _connectionFactory;

        public BlockchainSchemaBuilder(ILogger<BlockchainSchemaBuilder> logger,
            IBlockchainDbConnectionFactory connectionFactory)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> Provision(string blockchainId, DoubleSpendingProtectionType blockchainDoubleSpendingProtectionType)
        {
            _logger.LogInformation("DB schema for {@blockchainId} is being provisioned...", blockchainId);

            await using var connection = await _connectionFactory.Create(blockchainId);
            await using var transaction = await connection.BeginTransactionAsync();

            if (await CheckSchema(blockchainId, connection))
            {
                _logger.LogInformation("DB schema for {@blockchainId} already provisioned", blockchainId);

                return false;
            }

            await CreateCommonSchema(blockchainId, connection);

            switch (blockchainDoubleSpendingProtectionType)
            {
                case DoubleSpendingProtectionType.Coins:
                    await CreateCoinsSchema(blockchainId, connection);
                    break;

                case DoubleSpendingProtectionType.Nonce:
                    await CreateNonceSchema(blockchainId, connection);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockchainDoubleSpendingProtectionType), blockchainDoubleSpendingProtectionType, "");
            }

            await transaction.CommitAsync();

            _logger.LogInformation("DB schema for {@blockchainId} has been provisioned", blockchainId);

            return true;
        }

        public async Task UpgradeToOngoingIndexing(string blockchainId, DoubleSpendingProtectionType blockchainDoubleSpendingProtectionType)
        {
            _logger.LogInformation("DB schema for {@blockchainId} is being upgraded to ongoing indexing...", blockchainId);

            await using var connection = await _connectionFactory.Create(blockchainId);
            await using var transaction = await connection.BeginTransactionAsync();

            await UpgradeCommonSchemaToOngoingIndexing(blockchainId, connection);

            switch (blockchainDoubleSpendingProtectionType)
            {
                case DoubleSpendingProtectionType.Coins:
                    await UpgradeCoinsSchemaToOngoingIndexing(blockchainId, connection);
                    break;

                case DoubleSpendingProtectionType.Nonce:
                    await UpgradeNonceSchemaToOngoingIndexing(blockchainId, connection);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(blockchainDoubleSpendingProtectionType), blockchainDoubleSpendingProtectionType, "");
            }

            await transaction.CommitAsync();

            _logger.LogInformation("DB schema for {@blockchainId} has been upgraded to ongoing indexing", blockchainId);
        }

        private async Task CreateCommonSchema(string blockchainId, NpgsqlConnection connection)
        {
            _logger.LogInformation("Common DB schema for {@blockchainId} is being created...", blockchainId);
            
            var query = await LoadScript("before-indexing.sql");

            query = query.Replace("@schemaName", DbSchema.GetName(blockchainId));

            await connection.ExecuteAsync(query);

            _logger.LogInformation("Common DB schema for {@blockchainId} has been created", blockchainId);
        }

        private async Task CreateCoinsSchema(string blockchainId, NpgsqlConnection connection)
        {
            _logger.LogInformation("Coins DB schema for {@blockchainId} is being created...", blockchainId);
            
            var query = await LoadScript("Coins.before-coins-indexing.sql");

            query = query.Replace("@schemaName", DbSchema.GetName(blockchainId));

            await connection.ExecuteAsync(query);

            _logger.LogInformation("Coins DB schema for {@blockchainId} has been created", blockchainId);
        }

        private async Task CreateNonceSchema(string blockchainId, NpgsqlConnection connection)
        {
            _logger.LogInformation("Nonce DB schema for {@blockchainId} is being created...", blockchainId);
            
            var query = await LoadScript("Nonce.before-nonce-indexing.sql");

            query = query.Replace("@schemaName", DbSchema.GetName(blockchainId));

            await connection.ExecuteAsync(query);

            _logger.LogInformation("Nonce DB schema for {@blockchainId} has been created", blockchainId);
        }

        private async Task UpgradeCommonSchemaToOngoingIndexing(string blockchainId, NpgsqlConnection connection)
        {
            _logger.LogInformation("Common DB schema for {@blockchainId} is being upgraded to ongoing indexing...", blockchainId);

            var query = await LoadScript("before-ongoing-indexing.sql");

            query = query.Replace("@schemaName", DbSchema.GetName(blockchainId));

            await connection.ExecuteAsync(query);

            _logger.LogInformation("Common DB schema for {@blockchainId} has been upgraded to ongoing indexing", blockchainId);
        }

        private async Task UpgradeCoinsSchemaToOngoingIndexing(string blockchainId, NpgsqlConnection connection)
        {
            _logger.LogInformation("Coins DB schema for {@blockchainId} is being upgraded to ongoing indexing...", blockchainId);

            var query = await LoadScript("Coins.before-coins-ongoing-indexing.sql");

            query = query.Replace("@schemaName", DbSchema.GetName(blockchainId));

            await connection.ExecuteAsync(query);

            _logger.LogInformation("Coins DB schema for {@blockchainId} has been upgraded to ongoing indexing", blockchainId);
        }

        private async Task UpgradeNonceSchemaToOngoingIndexing(string blockchainId, NpgsqlConnection connection)
        {
            _logger.LogInformation("Nonce DB schema for {@blockchainId} is being upgraded to ongoing indexing...", blockchainId);

            var query = await LoadScript("Nonce.before-nonce-ongoing-indexing.sql");

            query = query.Replace("@schemaName", DbSchema.GetName(blockchainId));

            await connection.ExecuteAsync(query);

            _logger.LogInformation("Nonce DB schema for {@blockchainId} has been upgraded to ongoing indexing", blockchainId);
        }

        private static async Task<bool> CheckSchema(string blockchainId, NpgsqlConnection connection)
        {
            var query = "select exists (select * from pg_catalog.pg_namespace where nspname = @schemaName)";

            var isExists = await connection.ExecuteScalarAsync<bool>(query, new {schemaName = DbSchema.GetName(blockchainId)});

            return isExists;
        }

        private static async Task<string> LoadScript(string fileName)
        {
            return await SqlScriptsLoader.Load($"Initialization.{fileName}");
        }
    }
}
