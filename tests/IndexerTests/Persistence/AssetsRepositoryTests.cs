using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Persistence.Entities.Assets;
using IndexerTests.Sdk;
using IndexerTests.Sdk.Fixtures;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Swisschain.Sirius.Sdk.Primitives;
using Xunit;

namespace IndexerTests.Persistence
{
    public class AssetsRepositoryTests : PersistenceTests
    {
        public AssetsRepositoryTests(PersistenceFixture fixture) : 
            base(fixture)
        {
        }

        [Theory]
        [InlineData("zOPT0000007\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000", "zOPT0000007", "0x4C198D12F83226be4cfa44362509cB4a387a09C2")]
        [InlineData("aa\"bb", "aa\"bb", "0x4C198D12F83226be4cfa44362509cB4a387a09C2")]
        [InlineData("aa'bb", "aa'bb", "0x4C198D12F83226be4cfa44362509cB4a387a09C2")]
        public async Task CanAddAndGetAssetWithControlCharactersInSymbol(string symbol, string expectedSymbol, string address)
        {
            await using var commonDbContext = Fixture.CommonDbContextFactory.Invoke();
            await commonDbContext.Database.MigrateAsync();

            var assetsRepository = new AssetsRepository(Fixture.CommonDbContextFactory);

            var assetIds = new[] {new BlockchainAssetId(symbol, address)};
            var assets = new[] {new BlockchainAsset(assetIds[0], 10)};

            await assetsRepository.Add("blockchain1", assets);
            var result = (await assetsRepository.GetExisting("blockchain1", assetIds)).ToArray();

            result.Length.ShouldBe(1);
            result[0].BlockchainId.ShouldBe("blockchain1");
            result[0].Accuracy.ShouldBe(10);
            result[0].Id.ShouldNotBe(0);
            result[0].Symbol.ShouldBe(expectedSymbol);
            result[0].Address.ShouldBe(address);
        }
    }
}
