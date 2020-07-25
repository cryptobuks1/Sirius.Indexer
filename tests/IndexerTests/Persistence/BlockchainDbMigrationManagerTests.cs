﻿using System.Threading.Tasks;
using Indexer.Common.Persistence.BlockchainDbMigrations;
using Indexer.Common.Persistence.Entities.Blockchains;
using Indexer.Common.ReadModel.Blockchains;
using IndexerTests.Sdk;
using IndexerTests.Sdk.Fixtures;
using IndexerTests.Sdk.Mocks.Domain;
using Microsoft.Extensions.Logging.Abstractions;
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
        public async Task CanUpgradeCoinsSchemaToOngoingIndexing(string blockchainId, DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            var schemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, Fixture.BlockchainDbConnectionFactory);

            await schemaBuilder.ProvisionForIndexing(blockchainId, doubleSpendingProtectionType);
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
                registry);

            await manager.Migrate(blockchainId);
        }
    }
}
