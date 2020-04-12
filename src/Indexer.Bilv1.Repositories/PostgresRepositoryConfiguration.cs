namespace Indexer.Bilv1.Repositories
{
    public class PostgresBilV1RepositoryConfiguration
    {
        public static string SchemaName { get; } = "indexer_bil_v1";

        public static string MigrationHistoryTable { get; } = "__EFMigrationsHistory";
    }
}
