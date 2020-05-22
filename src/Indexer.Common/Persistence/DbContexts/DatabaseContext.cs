using System.Collections.Generic;
using Indexer.Common.Persistence.Entities;
using Indexer.Common.ReadModel.Blockchains;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swisschain.Extensions.Idempotency.EfCore;
using Swisschain.Sirius.Sdk.Primitives;

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
        public DbSet<BlockEntity> Blocks { get; set; }
        public DbSet<OutboxEntity> Outbox { get; set; }

        #region ReadModel

        public DbSet<BlockchainMetamodel> Blockchains { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);

            modelBuilder.BuildOutbox();

            BuildBlockchain(modelBuilder);
            BuildObservedOperation(modelBuilder);
            BuildBlocks(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void BuildBlocks(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlockEntity>()
                .ToTable("blocks")
                .HasKey(x => x.GlobalId);

            modelBuilder.Entity<BlockEntity>()
                .HasIndex(x => new
                {
                    x.BlockchainId,
                    x.Id
                })
                .IsUnique()
                .HasName("IX_Blocks_Blockchain_Id");

            modelBuilder.Entity<BlockEntity>()
                .HasIndex(x => new
                {
                    x.BlockchainId,
                    x.Number
                })
                .IsUnique()
                .HasName("IX_Blocks_Blockchain_Number");

            modelBuilder.Entity<BlockEntity>()
                .Property(x => x.BlockchainId)
                .IsRequired();

            modelBuilder.Entity<BlockEntity>()
                .Property(x => x.Id)
                .IsRequired();
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

            var jsonSerializingSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            #region Conversions

            modelBuilder.Entity<ObservedOperationEntity>().Property(e => e.Fees).HasConversion(
                v => JsonConvert.SerializeObject(v,
                    jsonSerializingSettings),
                v =>
                    JsonConvert.DeserializeObject<IReadOnlyCollection<Unit>>(v,
                        jsonSerializingSettings));

            #endregion
        }

        private static void BuildBlockchain(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlockchainMetamodel>()
                .ToTable("blockchains")
                .HasKey(x => x.Id);

            var jsonSerializingSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            modelBuilder.Entity<BlockchainMetamodel>().Property(e => e.Protocol).HasConversion(
                v => JsonConvert.SerializeObject(v,
                    jsonSerializingSettings),
                v =>
                    JsonConvert.DeserializeObject<Protocol>(v,
                        jsonSerializingSettings));
        }
    }
}
