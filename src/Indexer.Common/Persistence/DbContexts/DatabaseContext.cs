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
        public DbSet<FirstPassIndexerEntity> FirstPassHistoryIndexers { get; set; }
        public DbSet<SecondPassIndexerEntity> SecondPassIndexers { get; set; }
        public DbSet<OngoingIndexerEntity> OngoingIndexers { get; set; }
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
            BuildFirstPassIndexers(modelBuilder);
            BuildSecondPassIndexers(modelBuilder);
            BuildOngoingIndexers(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private static void BuildOngoingIndexers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OngoingIndexerEntity>(e =>
            {
                e.ToTable(TableNames.OngoingIndexers);
                e.HasKey(x => x.BlockchainId);

                e.Property(p => p.Version)
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            });
        }

        private static void BuildSecondPassIndexers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SecondPassIndexerEntity>(e =>
            {
                e.ToTable(TableNames.SecondPassIndexers);
                e.HasKey(x => x.BlockchainId);

                e.Property(p => p.Version)
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            });
        }

        private static void BuildFirstPassIndexers(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FirstPassIndexerEntity>(e =>
            {
                e.ToTable(TableNames.FirstPassIndexers);
                e.HasKey(x => x.Id);

                e.HasIndex(x => x.BlockchainId).HasName("IX_FirstPassIndexers_Blockchain");

                e.Property(x => x.BlockchainId).IsRequired();
                
                e.Property(p => p.Version)
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
            });
        }

        private static void BuildBlocks(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlockEntity>(e =>
            {
                e.ToTable(TableNames.Blocks);
                e.HasKey(x => x.GlobalId);

                e.HasIndex(x => new
                    {
                        x.BlockchainId,
                        x.Id
                    })
                    .IsUnique()
                    .HasName("IX_Blocks_Blockchain_Id");

                e.HasIndex(x => new
                    {
                        x.BlockchainId,
                        x.Number
                    })
                    .IsUnique()
                    .HasName("IX_Blocks_Blockchain_Number");

                e.Property(x => x.BlockchainId).IsRequired();
                e.Property(x => x.Id).IsRequired();
            });
        }

        private static void BuildObservedOperation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ObservedOperationEntity>()
                .ToTable(TableNames.ObserverOperations)
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
                .ToTable(TableNames.Blockchains)
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
