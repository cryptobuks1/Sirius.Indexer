using System.Collections.Generic;
using Indexer.Common.Persistence.Entities;
using Indexer.Common.ReadModel.Blockchains;
using Indexer.Common.Telemetry;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Swisschain.Extensions.Idempotency.EfCore;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.Persistence.EntityFramework
{
    public class DatabaseContext : DbContext, IDbContextWithOutbox
    {
        public const string SchemaName = "indexer";
        public const string MigrationHistoryTable = "__EFMigrationsHistory";

        public DatabaseContext(DbContextOptions<DatabaseContext> options, IAppInsight appInsight) :
            base(options)
        {
            AppInsight = appInsight;
        }

        public IAppInsight AppInsight { get; }

        public DbSet<ObservedOperationEntity> ObservedOperations { get; set; }
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

                e.HasIndex(x => x.BlockchainId).HasName("IX_FirstPassIndexers_BlockchainId");

                e.Property(x => x.BlockchainId).IsRequired();
                
                e.Property(p => p.Version)
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();
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
