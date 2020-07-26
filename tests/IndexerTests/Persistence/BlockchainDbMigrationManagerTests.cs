using System.Threading.Tasks;
using Indexer.Common.Persistence.BlockchainDbMigrations;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.ReadModel.Blockchains;
using IndexerTests.Sdk;
using IndexerTests.Sdk.Fixtures;
using IndexerTests.Sdk.Mocks.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Swisschain.Sirius.Sdk.Primitives;
using Xunit;

namespace IndexerTests.Persistence
{
    public class BlockchainDbMigrationManagerTests : PersistenceTests
    {
        public BlockchainDbMigrationManagerTests(PersistenceFixture fixture) : 
            base(fixture)
        {
        }

        [Theory]
        [InlineData("test-coins", DoubleSpendingProtectionType.Coins)]
        [InlineData("test-nonce", DoubleSpendingProtectionType.Nonce)]
        public async Task CanMigrateEmptySchema(string blockchainId, DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            var schemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, Fixture.BlockchainDbConnectionFactory);
            var registry = BlockchainDbMigrationsRegistryFactory.Create();
            var blockchainMetamodelProvider = new BlockchainMetamodelProviderMock
            {
                Metamodel = new BlockchainMetamodel
                {
                    Id = blockchainId,
                    Protocol = new Protocol {DoubleSpendingProtectionType = doubleSpendingProtectionType}
                }
            };
            
            var manager = new BlockchainDbMigrationManager(
                NullLogger<BlockchainDbMigrationManager>.Instance,
                Fixture.BlockchainDbConnectionFactory,
                blockchainMetamodelProvider,
                schemaBuilder,
                registry);

            await manager.Migrate(blockchainId);
        }

        [Theory]
        [InlineData("test-coins", DoubleSpendingProtectionType.Coins)]
        [InlineData("test-nonce", DoubleSpendingProtectionType.Nonce)]
        public async Task CanMigrateProvisionedSchema(string blockchainId, DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            var schemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, Fixture.BlockchainDbConnectionFactory);

            var provisionResult = await schemaBuilder.Provision(blockchainId, doubleSpendingProtectionType);

            provisionResult.ShouldBeTrue();

            var registry = BlockchainDbMigrationsRegistryFactory.Create();
            var blockchainMetamodelProvider = new BlockchainMetamodelProviderMock
            {
                Metamodel = new BlockchainMetamodel
                {
                    Id = blockchainId,
                    Protocol = new Protocol {DoubleSpendingProtectionType = doubleSpendingProtectionType}
                }
            };
            
            var manager = new BlockchainDbMigrationManager(
                NullLogger<BlockchainDbMigrationManager>.Instance,
                Fixture.BlockchainDbConnectionFactory,
                blockchainMetamodelProvider,
                schemaBuilder,
                registry);

            await manager.Migrate(blockchainId);
        }

        [Theory]
        [InlineData("test-coins", DoubleSpendingProtectionType.Coins)]
        [InlineData("test-nonce", DoubleSpendingProtectionType.Nonce)]
        public async Task CanMigrateProvisionedSchemaUpdatedForOngoingIndexing(string blockchainId, DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            var schemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, Fixture.BlockchainDbConnectionFactory);

            var provisionResult = await schemaBuilder.Provision(blockchainId, doubleSpendingProtectionType);

            provisionResult.ShouldBeTrue();

            await schemaBuilder.UpgradeToOngoingIndexing(blockchainId, doubleSpendingProtectionType);

            var registry = BlockchainDbMigrationsRegistryFactory.Create();
            var blockchainMetamodelProvider = new BlockchainMetamodelProviderMock
            {
                Metamodel = new BlockchainMetamodel
                {
                    Id = blockchainId,
                    Protocol = new Protocol {DoubleSpendingProtectionType = doubleSpendingProtectionType}
                }
            };
            
            var manager = new BlockchainDbMigrationManager(
                NullLogger<BlockchainDbMigrationManager>.Instance,
                Fixture.BlockchainDbConnectionFactory,
                blockchainMetamodelProvider,
                schemaBuilder,
                registry);

            await manager.Migrate(blockchainId);
        }
    }
}
