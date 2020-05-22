﻿using System.Collections.Generic;
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
        public const string SchemaName = "indexer-2";
        public const string MigrationHistoryTable = "__EFMigrationsHistory";

        public DatabaseContext(DbContextOptions<DatabaseContext> options) :
            base(options)
        {
        }

        public DbSet<ObservedOperationEntity> ObservedOperations { get; set; }

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
