using System.Threading.Tasks;
using Indexer.Common.Persistence.Entities.BlockchainDbMigrations;
using IndexerTests.Sdk;
using IndexerTests.Sdk.Fixtures;
using Shouldly;
using Xunit;

namespace IndexerTests.Persistence
{
    public class MigrationsRepositoryTests : PersistenceTests
    {
        public MigrationsRepositoryTests(PersistenceFixture fixture) :
            base(fixture)
        {
        }

        [Fact]
        public async Task CanGetVersionIfThereAreNoMigrationsTable()
        {
            await using var connection = await Fixture.CreateConnection();
            var repo = new BlockchainDbMigrationsRepository(connection, "test");

            var version = await repo.GetMaxVersion();

            version.ShouldBe(0);
        }
    }
}
