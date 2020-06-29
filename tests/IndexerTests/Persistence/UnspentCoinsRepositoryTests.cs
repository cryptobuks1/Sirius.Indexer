using System;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Transactions;
using Indexer.Common.Domain.Transactions.Transfers;
using Indexer.Common.Persistence;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using Indexer.Common.Persistence.Entities.TransactionHeaders;
using Indexer.Common.Persistence.Entities.UnspentCoins;
using Indexer.Common.Telemetry;
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
            await Fixture.SchemaBuilder.ProvisionForIndexing("test", DoubleSpendingProtectionType.Coins);

            var repo = new UnspentCoinsRepository(Fixture.BlockchainDbConnectionFactory);

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

        [Fact]
        public async Task CanGetByAddress()
        {
            await Fixture.SchemaBuilder.ProvisionForIndexing("test", DoubleSpendingProtectionType.Coins);
            await Fixture.SchemaBuilder.UpgradeToOngoingIndexing("test", DoubleSpendingProtectionType.Coins);

            var repo = new UnspentCoinsRepository(Fixture.BlockchainDbConnectionFactory);

            var generatedCoins = Enumerable
                .Range(0, 100)
                .Select(i => new UnspentCoin(
                    new CoinId("test-tx", i),
                    new Unit(1, 1),
                    i % 2 == 0 ? "address1" : "address2",
                    default,
                    default))
                .ToArray();

            await repo.InsertOrIgnore("test", generatedCoins);
            
            var readCoins = await repo.GetByAddress("test", "address1", null);

            readCoins.ShouldBe(generatedCoins.Where(x => x.Address == "address1"), ignoreOrder: true);
            readCoins.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task CanGetByAddressWithAsAtBlockNumber()
        {
            await Fixture.SchemaBuilder.ProvisionForIndexing("test-1", DoubleSpendingProtectionType.Coins);
            await Fixture.SchemaBuilder.UpgradeToOngoingIndexing("test-1", DoubleSpendingProtectionType.Coins);

            var repo = new UnspentCoinsRepository(Fixture.BlockchainDbConnectionFactory);
            var transactionHeadersRepo = new TransactionHeadersRepository(Fixture.BlockchainDbConnectionFactory, new TurnedOffAppInsight());
            var blockHeadersRepo = new BlockHeadersRepository(Fixture.BlockchainDbConnectionFactory, new TurnedOffAppInsight());

            var generatedBlocks = Enumerable
                .Range(0, 5)
                .Select(i => new BlockHeader(
                    "test-1",
                    $"test-block-{i}",
                    i,
                    i == 0 ? null : $"test-block-{i - 1}",
                    DateTime.UtcNow))
                .ToArray();

            var generatedTransactions = Enumerable
                .Range(0, 10)
                .Select(i => TransactionHeader.Create(
                    "test-1",
                    $"test-block-{i % 5}",
                    $"test-tx-{i}",
                    i,
                    default))
                .ToArray();

            var generatedCoins = Enumerable
                .Range(0, 100)
                .Select(i => new UnspentCoin(
                    new CoinId($"test-tx-{i % 10}", i),
                    new Unit(1, 1),
                    i % 2 == 0 ? "address1" : "address2",
                    default,
                    default))
                .ToArray();

            foreach (var block in generatedBlocks)
            {
                await blockHeadersRepo.InsertOrIgnore(block);
            }

            await transactionHeadersRepo.InsertOrIgnore(generatedTransactions);
            await repo.InsertOrIgnore("test-1", generatedCoins);
            
            var readCoins = await repo.GetByAddress("test-1", "address1", 2);

            var expectedCoins = generatedCoins
                .Where(c =>
                    c.Address == "address1" &&
                    generatedTransactions.Any(t =>
                        t.Id == c.Id.TransactionId &&
                        generatedBlocks.Any(b => b.Id == t.BlockId && b.Number <= 2)))
                .ToArray();

            readCoins.ShouldBe(expectedCoins, ignoreOrder: true);
            readCoins.ShouldNotBeEmpty();
        }
    }
}
