using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.UnspentCoins;
using IndexerTests.Sdk;
using IndexerTests.Sdk.Fixtures;
using Shouldly;
using Swisschain.Sirius.Sdk.Primitives;
using Xunit;

namespace IndexerTests.Persistence
{
    public class UnspentCoinsRepositoryTests : PersistenceTests
    {
        public UnspentCoinsRepositoryTests(PersistenceFixture fixture) :
            base(fixture)
        {
        }

        [Fact]
        public async Task CanReadAndRemoveMoreThan1000CoinsByIdsList()
        {
            await Fixture.CreateSchema("test", DoubleSpendingProtectionType.Coins);

            var repo = new UnspentCoinsRepository(Fixture.CreateConnection);

            var generatedCoins = Enumerable
                .Range(0, NpgSqlConnectionQueryExtensions.BatchSize + 50)
                .Select(i => new UnspentCoin(
                    new CoinId("test-tx", i),
                    new Unit(1, 1),
                    "address",
                    default,
                    default))
                .ToArray();

            await repo.InsertOrIgnore("test", generatedCoins);
            var ids = generatedCoins.Select(x => x.Id).ToArray();

            var readCoins = await repo.GetAnyOf("test", ids);

            await repo.Remove("test", ids);

            var readCoins2 = await repo.GetAnyOf("test", ids);

            readCoins.ShouldBe(generatedCoins, ignoreOrder: true);
            readCoins2.ShouldBeEmpty();
        }
    }
}
