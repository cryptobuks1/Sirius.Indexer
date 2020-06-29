using Indexer.Common.Persistence.Entities.Assets;
using Indexer.Common.Persistence.Entities.FirstPassIndexers;
using Indexer.Common.Persistence.Entities.OngoingIndexers;
using Indexer.Common.Persistence.Entities.SecondPassIndexers;
using Indexer.Common.ReadModel.Blockchains;
using Indexer.Common.Telemetry;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Indexer.Common.Persistence.EntityFramework
{
    public class CommonDatabaseContext : DbContext
    {
        public const string SchemaName = "indexer";
        public const string MigrationHistoryTable = "__EFMigrationsHistory";

        public CommonDatabaseContext(DbContextOptions<CommonDatabaseContext> options, IAppInsight appInsight) :
            base(options)
        {
            AppInsight = appInsight;
        }

        public IAppInsight AppInsight { get; }
        public DbSet<FirstPassIndexerEntity> FirstPassHistoryIndexers { get; set; }
        public DbSet<SecondPassIndexerEntity> SecondPassIndexers { get; set; }
        public DbSet<OngoingIndexerEntity> OngoingIndexers { get; set; }
        public DbSet<AssetEntity> Assets { get; set; }

        #region Read models

        public DbSet<BlockchainMetamodel> Blockchains { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(SchemaName);

            BuildBlockchain(modelBuilder);
            BuildFirstPassIndexers(modelBuilder);
            BuildSecondPassIndexers(modelBuilder);
            BuildOngoingIndexers(modelBuilder);
            BuildAssets(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void BuildAssets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssetEntity>(e =>
            {
                e.ToTable(TableNames.Assets);
                e.HasKey(x => x.Id);

                e.Property(x => x.Id)
                    .HasIdentityOptions(100_000)
                    .ValueGeneratedOnAdd();

                e.HasIndex(x => x.BlockchainId)
                    .HasName("ix_assets_blockchain_id");

                e.HasIndex(x => x.Symbol)
                    .IsUnique()
                    .HasFilter($"\"{nameof(AssetEntity.Address)}\" is null")
                    .HasName("ix_assets_symbol");

                e.HasIndex(x => new
                    {
                        x.Symbol,
                        x.Address
                    })
                    .IsUnique()
                    .HasFilter($"\"{nameof(AssetEntity.Address)}\" is not null")
                    .HasName("ix_assets_symbol_address");
            });
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
