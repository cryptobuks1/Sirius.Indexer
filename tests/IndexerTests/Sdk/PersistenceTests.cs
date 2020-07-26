using System.Threading.Tasks;
using IndexerTests.Sdk.Fixtures;
using Xunit;

namespace IndexerTests.Sdk
{
    [Collection(nameof(PersistenceTests))]
    public abstract class PersistenceTests : 
        IClassFixture<PersistenceFixture>,
        IAsyncLifetime
    {
        public PersistenceTests(PersistenceFixture fixture)
        {
            Fixture = fixture;
        }

        public PersistenceFixture Fixture { get; }

        public async Task InitializeAsync()
        {
            await Fixture.CreateTestDb();
        }

        public async Task DisposeAsync()
        {
            await Fixture.DropTestDb();
        }
    }
}
