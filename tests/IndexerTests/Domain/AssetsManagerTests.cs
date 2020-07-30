using System.Threading.Tasks;
using Indexer.Common.Domain.Assets;
using Indexer.Common.Persistence.Entities.Assets;
using IndexerTests.Sdk;
using IndexerTests.Sdk.Fixtures;
using IndexerTests.Sdk.Mocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Swisschain.Sirius.Sdk.Primitives;
using Xunit;

namespace IndexerTests.Domain
{
    public class AssetsManagerTests : PersistenceTests
    {
        public AssetsManagerTests(PersistenceFixture fixture) : 
            base(fixture)
        {
        }

        [Theory]
        [InlineData("AAA", null)]
        [InlineData("AAA", "123")]
        public async Task CanManageTheSameAssetInDifferentBlockchains(string symbol, string address)
        {
            await using var commonDbContext = Fixture.CommonDbContextFactory.Invoke();
            await commonDbContext.Database.MigrateAsync();

            var assetsRepository = new AssetsRepository(Fixture.CommonDbContextFactory);
            
            var manager = new AssetsManager(assetsRepository, new DummyMemoryPublisher());

            var blockchainAssetId = new BlockchainAssetId(symbol, address);

            var result1 = await manager.EnsureAdded("blockchain1", new[] {new BlockchainAsset(blockchainAssetId, 8)});
            var result2 = await manager.EnsureAdded("blockchain2", new[] {new BlockchainAsset(blockchainAssetId, 8)});

            result1.ShouldHaveSingleItem();
            result1.ContainsKey(blockchainAssetId).ShouldBeTrue();
            result1[blockchainAssetId].Symbol.ShouldBe(blockchainAssetId.Symbol);
            result1[blockchainAssetId].Address.ShouldBe(blockchainAssetId.Address);
            result1[blockchainAssetId].Accuracy.ShouldBe(8);
            result1[blockchainAssetId].BlockchainId.ShouldBe("blockchain1");
            result1[blockchainAssetId].Id.ShouldNotBe(0L);

            result2.ShouldHaveSingleItem();
            result2.ContainsKey(blockchainAssetId).ShouldBeTrue();
            result2[blockchainAssetId].Symbol.ShouldBe(blockchainAssetId.Symbol);
            result2[blockchainAssetId].Address.ShouldBe(blockchainAssetId.Address);
            result2[blockchainAssetId].Accuracy.ShouldBe(8);
            result2[blockchainAssetId].BlockchainId.ShouldBe("blockchain2");
            result2[blockchainAssetId].Id.ShouldNotBe(0L);

            result1[blockchainAssetId].Id.ShouldNotBe(result2[blockchainAssetId].Id);

            var blockchain1Assets = await assetsRepository.GetAllAsync("blockchain1");
            var blockchain2Assets = await assetsRepository.GetAllAsync("blockchain2");

            blockchain1Assets.ShouldHaveSingleItem();
            blockchain2Assets.ShouldHaveSingleItem();
        }
    }
}
