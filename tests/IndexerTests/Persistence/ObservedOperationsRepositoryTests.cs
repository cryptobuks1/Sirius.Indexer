using System.Threading.Tasks;
using Indexer.Common.Domain.ObservedOperations;
using IndexerTests.Sdk;
using IndexerTests.Sdk.Fixtures;
using Swisschain.Sirius.Sdk.Primitives;
using Xunit;

namespace IndexerTests.Persistence
{
    public class ObservedOperationsRepositoryTests : PersistenceTests
    {
        public ObservedOperationsRepositoryTests(PersistenceFixture fixture) : 
            base(fixture)
        {
        }

        [Theory]
        [InlineData(DoubleSpendingProtectionType.Coins)]
        [InlineData(DoubleSpendingProtectionType.Nonce)]
        public async Task CanAddOperationSeveralTimes(DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            await Fixture.SchemaBuilder.ProvisionForIndexing("test", doubleSpendingProtectionType);

            await using var dbUnitOfWork = await Fixture.BlockchainDbUnitOfWorkFactory.Start("test");

            var operation = ObservedOperation.Create(100, "test", "tx-100");

            await dbUnitOfWork.ObservedOperations.AddOrIgnore(operation);
            await dbUnitOfWork.ObservedOperations.AddOrIgnore(operation);
        }
    }
}
