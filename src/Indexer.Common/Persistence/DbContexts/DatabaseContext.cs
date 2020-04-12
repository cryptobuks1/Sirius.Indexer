using Indexer.Common.ReadModel.Blockchains;
using Microsoft.EntityFrameworkCore;

namespace Indexer.Common.Persistence.DbContexts
{
    public class DatabaseContext : DbContext
    {
        public const string SchemaName = "indexer";
        public const string MigrationHistoryTable = "__EFMigrationsHistory";

        public DatabaseContext(DbContextOptions<DatabaseContext> options) :
            base(options)
        {
        }
        public DbSet<Blockchain> Blockchains { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);

            BuildBlockchain(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void BuildBlockchain(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blockchain>()
                .ToTable("blockchains")
                .HasKey(x => x.BlockchainId);
        }
    }
}
