using System.Linq;
using Indexer.Common.Persistence.BlockchainDbMigrations;
using Shouldly;
using Swisschain.Sirius.Sdk.Primitives;
using Xunit;

namespace IndexerTests.UnitTests
{
    public class BlockchainDbMigrationsRegistryTests
    {
        [Fact]
        public void CanReturnWhenEmpty()
        {
            var registry = new BlockchainDbMigrationsRegistry();

            var pending = registry.GetPending(0, DoubleSpendingProtectionType.Nonce);

            pending.ShouldBeEmpty();
        }

        [Fact]
        public void CanAddOne()
        {
            var registry = new BlockchainDbMigrationsRegistry();

            registry.Add(MigrationTargetBlockchainType.All, "migration1.sql");

            var pending = registry.GetPending(0, DoubleSpendingProtectionType.Nonce);

            pending.ShouldHaveSingleItem();
            pending.Single().Version.ShouldBe(1);
            pending.Single().TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pending.Single().ScriptPath.ShouldBe("migration1.sql");
        }

        [Fact]
        public void CanAddSeveral()
        {
            var registry = new BlockchainDbMigrationsRegistry();

            registry.Add(MigrationTargetBlockchainType.All, "migration1.sql");
            registry.Add(MigrationTargetBlockchainType.All, "migration2.sql");
            registry.Add(MigrationTargetBlockchainType.All, "migration3.sql");

            var pending = registry.GetPending(0, DoubleSpendingProtectionType.Nonce).ToArray();

            pending.Length.ShouldBe(3);
            
            pending[0].Version.ShouldBe(1);
            pending[0].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pending[0].ScriptPath.ShouldBe("migration1.sql");

            pending[1].Version.ShouldBe(2);
            pending[1].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pending[1].ScriptPath.ShouldBe("migration2.sql");

            pending[2].Version.ShouldBe(3);
            pending[2].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pending[2].ScriptPath.ShouldBe("migration3.sql");
        }

        [Theory]
        [InlineData(DoubleSpendingProtectionType.Coins)]
        [InlineData(DoubleSpendingProtectionType.Nonce)]
        public void MigrationWhichTargetsAllBlockchainsReturnsForAllBlockchainTypes(DoubleSpendingProtectionType doubleSpendingProtectionType)
        {
            var registry = new BlockchainDbMigrationsRegistry();

            registry.Add(MigrationTargetBlockchainType.All, "migration1.sql");

            var pending = registry.GetPending(0, doubleSpendingProtectionType);

            pending.ShouldHaveSingleItem();
        }

        [Fact]
        public void MigrationWhichTargetsCoinsBlockchainDoesNotReturnForNonceBlockchain()
        {
            var registry = new BlockchainDbMigrationsRegistry();

            registry.Add(MigrationTargetBlockchainType.Coins, "migration1.sql");

            var pending = registry.GetPending(0, DoubleSpendingProtectionType.Nonce);

            pending.ShouldBeEmpty();
        }

        [Fact]
        public void MigrationWhichTargetsNonceBlockchainDoesNotReturnForCoinsBlockchain()
        {
            var registry = new BlockchainDbMigrationsRegistry();

            registry.Add(MigrationTargetBlockchainType.Nonce, "migration1.sql");

            var pending = registry.GetPending(0, DoubleSpendingProtectionType.Coins);

            pending.ShouldBeEmpty();
        }

        [Fact]
        public void CanFilterByVersion()
        {
            var registry = new BlockchainDbMigrationsRegistry();

            registry.Add(MigrationTargetBlockchainType.All, "migration1.sql");
            registry.Add(MigrationTargetBlockchainType.Nonce, "migration2.sql");
            registry.Add(MigrationTargetBlockchainType.Coins, "migration3.sql");
            registry.Add(MigrationTargetBlockchainType.Nonce, "migration4.sql");
            registry.Add(MigrationTargetBlockchainType.Nonce, "migration5.sql");
            registry.Add(MigrationTargetBlockchainType.Coins, "migration6.sql");
            registry.Add(MigrationTargetBlockchainType.All, "migration7.sql");

            var pending = registry.GetPending(3, DoubleSpendingProtectionType.Nonce).ToArray();

            pending.Length.ShouldBe(3);
            
            pending[0].Version.ShouldBe(4);
            pending[0].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.Nonce);
            pending[0].ScriptPath.ShouldBe("migration4.sql");

            pending[1].Version.ShouldBe(5);
            pending[1].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.Nonce);
            pending[1].ScriptPath.ShouldBe("migration5.sql");

            pending[2].Version.ShouldBe(7);
            pending[2].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pending[2].ScriptPath.ShouldBe("migration7.sql");
        }

        [Fact]
        public void ComplexCase()
        {
            var registry = new BlockchainDbMigrationsRegistry();

            registry.Add(MigrationTargetBlockchainType.All, "migration1.sql");
            registry.Add(MigrationTargetBlockchainType.Nonce, "migration2.sql");
            registry.Add(MigrationTargetBlockchainType.Coins, "migration3.sql");
            registry.Add(MigrationTargetBlockchainType.Nonce, "migration4.sql");
            registry.Add(MigrationTargetBlockchainType.Nonce, "migration5.sql");
            registry.Add(MigrationTargetBlockchainType.Coins, "migration6.sql");
            registry.Add(MigrationTargetBlockchainType.All, "migration7.sql");

            var pendingForNonce = registry.GetPending(0, DoubleSpendingProtectionType.Nonce).ToArray();
            var pendingForCoins = registry.GetPending(0, DoubleSpendingProtectionType.Coins).ToArray();

            pendingForNonce.Length.ShouldBe(5);
            
            pendingForNonce[0].Version.ShouldBe(1);
            pendingForNonce[0].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pendingForNonce[0].ScriptPath.ShouldBe("migration1.sql");

            pendingForNonce[1].Version.ShouldBe(2);
            pendingForNonce[1].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.Nonce);
            pendingForNonce[1].ScriptPath.ShouldBe("migration2.sql");

            pendingForNonce[2].Version.ShouldBe(4);
            pendingForNonce[2].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.Nonce);
            pendingForNonce[2].ScriptPath.ShouldBe("migration4.sql");

            pendingForNonce[3].Version.ShouldBe(5);
            pendingForNonce[3].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.Nonce);
            pendingForNonce[3].ScriptPath.ShouldBe("migration5.sql");

            pendingForNonce[4].Version.ShouldBe(7);
            pendingForNonce[4].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pendingForNonce[4].ScriptPath.ShouldBe("migration7.sql");

            pendingForCoins.Length.ShouldBe(4);
            
            pendingForCoins[0].Version.ShouldBe(1);
            pendingForCoins[0].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pendingForCoins[0].ScriptPath.ShouldBe("migration1.sql");
            
            pendingForCoins[1].Version.ShouldBe(3);
            pendingForCoins[1].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.Coins);
            pendingForCoins[1].ScriptPath.ShouldBe("migration3.sql");
            
            pendingForCoins[2].Version.ShouldBe(6);
            pendingForCoins[2].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.Coins);
            pendingForCoins[2].ScriptPath.ShouldBe("migration6.sql");
            
            pendingForCoins[3].Version.ShouldBe(7);
            pendingForCoins[3].TargetBlockchainType.ShouldBe(MigrationTargetBlockchainType.All);
            pendingForCoins[3].ScriptPath.ShouldBe("migration7.sql");
        }
    }
}
