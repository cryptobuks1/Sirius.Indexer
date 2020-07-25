namespace Indexer.Common.Persistence.BlockchainDbMigrations
{
    internal static class BlockchainDbMigrationsRegistryFactory
    {
        public static BlockchainDbMigrationsRegistry Create()
        {
            var registry = new BlockchainDbMigrationsRegistry();

            registry.Add(MigrationTargetBlockchainType.All, "add-migrations.sql");
            registry.Add(MigrationTargetBlockchainType.Coins, "Coins.convert-coins-script-pub-key-to-text.sql");

            return registry;
        }
    }
}
