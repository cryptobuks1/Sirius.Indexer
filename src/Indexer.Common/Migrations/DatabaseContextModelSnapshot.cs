﻿// <auto-generated />
using System;
using Indexer.Common.Persistence.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Indexer.Common.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    partial class DatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("indexer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Indexer.Common.Persistence.Entities.BlockEntity", b =>
                {
                    b.Property<string>("GlobalId")
                        .HasColumnType("text");

                    b.Property<string>("BlockchainId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Id")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("Number")
                        .HasColumnType("bigint");

                    b.Property<string>("PreviousId")
                        .HasColumnType("text");

                    b.HasKey("GlobalId");

                    b.HasIndex("BlockchainId", "Id")
                        .IsUnique()
                        .HasName("IX_Blocks_Blockchain_Id");

                    b.HasIndex("BlockchainId", "Number")
                        .IsUnique()
                        .HasName("IX_Blocks_Blockchain_Number");

                    b.ToTable("blocks");
                });

            modelBuilder.Entity("Indexer.Common.Persistence.Entities.FirstPassIndexerEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("BlockchainId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("NextBlock")
                        .HasColumnType("bigint");

                    b.Property<long>("StartBlock")
                        .HasColumnType("bigint");

                    b.Property<long>("StopBlock")
                        .HasColumnType("bigint");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnName("xmin")
                        .HasColumnType("xid");

                    b.HasKey("Id");

                    b.HasIndex("BlockchainId")
                        .HasName("IX_FirstPassIndexers_Blockchain");

                    b.ToTable("first_pass_indexers");
                });

            modelBuilder.Entity("Indexer.Common.Persistence.Entities.ObservedOperationEntity", b =>
                {
                    b.Property<long>("OperationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<long>("AssetId")
                        .HasColumnType("bigint");

                    b.Property<Guid>("BilV1OperationId")
                        .HasColumnType("uuid");

                    b.Property<string>("BlockchainId")
                        .HasColumnType("text");

                    b.Property<string>("DestinationAddress")
                        .HasColumnType("text");

                    b.Property<string>("Fees")
                        .HasColumnType("text");

                    b.Property<bool>("IsCompleted")
                        .HasColumnType("boolean");

                    b.Property<decimal>("OperationAmount")
                        .HasColumnType("numeric");

                    b.Property<string>("TransactionId")
                        .HasColumnType("text");

                    b.HasKey("OperationId");

                    b.HasIndex("IsCompleted")
                        .HasName("IX_ObservedOperations_IsCompleted");

                    b.ToTable("observed_operations");
                });

            modelBuilder.Entity("Indexer.Common.ReadModel.Blockchains.BlockchainMetamodel", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("IntegrationUrl")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("NetworkType")
                        .HasColumnType("integer");

                    b.Property<string>("Protocol")
                        .HasColumnType("text");

                    b.Property<string>("TenantId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("blockchains");
                });

            modelBuilder.Entity("Swisschain.Extensions.Idempotency.EfCore.OutboxEntity", b =>
                {
                    b.Property<string>("RequestId")
                        .HasColumnType("text");

                    b.Property<long>("AggregateId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("Npgsql:IdentitySequenceOptions", "'2', '1', '', '', 'False', '1'")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("Commands")
                        .HasColumnType("text");

                    b.Property<string>("Events")
                        .HasColumnType("text");

                    b.Property<bool>("IsDispatched")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsStored")
                        .HasColumnType("boolean");

                    b.Property<string>("Response")
                        .HasColumnType("text");

                    b.HasKey("RequestId");

                    b.ToTable("outbox");
                });
#pragma warning restore 612, 618
        }
    }
}
