using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Indexer.Common.Domain.Blockchains;
using Indexer.Common.Persistence.Entities.BlockchainDbMigrations;
using Indexer.Common.Persistence.SqlScripts;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Indexer.Common.Persistence.BlockchainDbMigrations
{
    internal sealed class BlockchainDbMigrationManager : IBlockchainDbMigrationsManager
    {
        private readonly ILogger<BlockchainDbMigrationManager> _logger;
        private readonly IBlockchainDbConnectionFactory _blockchainDbConnectionFactory;
        private readonly IBlockchainMetamodelProvider _blockchainMetamodelProvider;
        private readonly BlockchainDbMigrationsRegistry _registry;

        public BlockchainDbMigrationManager(
            ILogger<BlockchainDbMigrationManager> logger,
            IBlockchainDbConnectionFactory blockchainDbConnectionFactory,
            IBlockchainMetamodelProvider blockchainMetamodelProvider,
            BlockchainDbMigrationsRegistry registry)
        {
            _logger = logger;
            _blockchainDbConnectionFactory = blockchainDbConnectionFactory;
            _blockchainMetamodelProvider = blockchainMetamodelProvider;
            _registry = registry;
        }

        public async Task Migrate(string blockchainId)
        {
            _logger.LogInformation("Blockchain {blockchainId} DB schema is being migrated...", blockchainId);

            await using var connection = await _blockchainDbConnectionFactory.Create(blockchainId);

            var blockchainMetamodel = await _blockchainMetamodelProvider.Get(blockchainId);
            var schema = DbSchema.GetName(blockchainId);
            var migrationsRepository = new BlockchainDbMigrationsRepository(connection, schema);

            var currentVersion = await migrationsRepository.GetMaxVersion();
            var pendingMigrations = _registry.GetPending(currentVersion, blockchainMetamodel.Protocol.DoubleSpendingProtectionType);

            foreach (var pendingMigration in pendingMigrations)
            {
                await ApplyMigration(connection, migrationsRepository, blockchainId, schema, pendingMigration);
            }

            _logger.LogInformation("Blockchain {blockchainId} DB schema has been migrated", blockchainId);
        }

        public async Task Validate(string blockchainId)
        {
            _logger.LogInformation("Blockchain {blockchainId} DB schema is being validated...", blockchainId);

            await using var connection = await _blockchainDbConnectionFactory.Create(blockchainId);

            var blockchainMetamodel = await _blockchainMetamodelProvider.Get(blockchainId);
            var schema = DbSchema.GetName(blockchainId);
            var migrationsRepository = new BlockchainDbMigrationsRepository(connection, schema);
            
            var currentVersion = await migrationsRepository.GetMaxVersion();
            var pendingMigrations = _registry.GetPending(currentVersion, blockchainMetamodel.Protocol.DoubleSpendingProtectionType);

            if (pendingMigrations.Any())
            {
                _logger.LogInformation("There are {pendingMigrationsCount} pending migration in blockchain {blockchainId} DB",
                    pendingMigrations.Count,
                    blockchainId);

                throw new InvalidOperationException($"There are {pendingMigrations.Count} pending migrations in blockchain {blockchainId} DB schema");
            }

            _logger.LogInformation("Blockchain {blockchainId} DB schema has been validated", blockchainId);
        }

        private async Task ApplyMigration(
            NpgsqlConnection connection,
            BlockchainDbMigrationsRepository migrationsRepository,
            string blockchainId,
            string schema,
            BlockchainDbMigration pendingMigration)
        {
            _logger.LogInformation("Applying blockchain {blockchainId} DB migration v{migrationVersion} {migrationScriptFilePath}...",
                blockchainId,
                pendingMigration.Version,
                pendingMigration.ScriptPath);

            var query = await SqlScriptsLoader.Load($"Migrations.{pendingMigration.ScriptPath}");

            query = query.Replace("@schemaName", schema);

            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                await connection.ExecuteAsync(query);
                await migrationsRepository.Add(pendingMigration);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Blockchain {blockchainId} DB migration v{migrationVersion} {migrationScriptFilePath} migration has been applied",
                blockchainId,
                pendingMigration.Version,
                pendingMigration.ScriptPath);
        }
    }
}
