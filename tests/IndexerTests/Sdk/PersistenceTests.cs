using IndexerTests.Sdk.Fixtures;
using Xunit;

namespace IndexerTests.Sdk
{
    [Collection(nameof(PersistenceTests))]
    public abstract class PersistenceTests : IClassFixture<PersistenceFixture>
    {
        public PersistenceTests(PersistenceFixture fixture)
        {
            Fixture = fixture;
        }

        public PersistenceFixture Fixture { get; }
    }
}
