using System.Threading.Tasks;
using Indexer.Common.Persistence.Entities.Blockchains;
using IndexerTests.Sdk;
using IndexerTests.Sdk.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Swisschain.Sirius.Sdk.Primitives;
using Xunit;

namespace IndexerTests.Persistence
{
    public class BlockchainSchemaBuilderTests : PersistenceTests
    {
        public BlockchainSchemaBuilderTests(PersistenceFixture fixture) :
            base(fixture)
        {
        }

        [Theory]
        [InlineData("test-coins", DoubleSpendingProtectionType.Coins)]
        [InlineData("test-nonce", DoubleSpendingProtectionType.Nonce)]
        public async Task CanProvisionCoinsSchema(string blockchainId, DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            var schemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, Fixture.CreateConnection);

            await schemaBuilder.ProvisionForIndexing(blockchainId, doubleSpendingProtectionType);
        }

        [Theory]
        [InlineData("test-coins", DoubleSpendingProtectionType.Coins)]
        [InlineData("test-nonce", DoubleSpendingProtectionType.Nonce)]
        public async Task CanUpgradeCoinsSchemaToOngoingIndexing(string blockchainId, DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            var schemaBuilder = new BlockchainSchemaBuilder(NullLogger<BlockchainSchemaBuilder>.Instance, Fixture.CreateConnection);

            await schemaBuilder.UpgradeToOngoingIndexing(blockchainId, doubleSpendingProtectionType);
        }
    }
}
