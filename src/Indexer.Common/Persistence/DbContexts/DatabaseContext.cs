using Indexer.Common.Domain.ObservedOperations;
using Indexer.Common.Persistence.Entities;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.EntityFrameworkCore;
using Swisschain.Extensions.Idempotency.EfCore;

namespace Indexer.Common.Persistence.DbContexts
{
    public class DatabaseContext : DbContext, IDbContextWithOutbox
    {
        public const string SchemaName = "indexer";
        public const string MigrationHistoryTable = "__EFMigrationsHistory";

        public DatabaseContext(DbContextOptions<DatabaseContext> options) :
            base(options)
        {
        }

        public DbSet<ObservedOperationEntity> ObservedOperations { get; set; }

        public DbSet<OutboxEntity> Outbox { get; set; }

        #region ReadModel

        public DbSet<Blockchain> Blockchains { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);

            modelBuilder.BuildOutbox();

            BuildBlockchain(modelBuilder);
            BuildObservedOperation(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void BuildObservedOperation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObservedOperationEntity>()
                .ToTable("observed_operations")
                .HasKey(x => x.OperationId);

            modelBuilder.Entity<ObservedOperationEntity>()
                .HasIndex(x => x.IsCompleted)
                .IsUnique(false)
                .HasName("IX_ObservedOperations_IsCompleted");
        }

        private static void BuildBlockchain(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blockchain>()
                .ToTable("blockchains")
                .HasKey(x => x.BlockchainId);
        }
    }
}
