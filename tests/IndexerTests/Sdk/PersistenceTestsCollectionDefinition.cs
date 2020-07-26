using IndexerTests.Sdk.Fixtures;
using Xunit;

namespace IndexerTests.Sdk
{
    [CollectionDefinition(nameof(PersistenceTests))]
    public sealed class PersistenceTestsCollectionDefinition : ICollectionFixture<PersistenceFixture>
    {
    }
}
