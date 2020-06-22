﻿// <auto-generated />
using System;
using Indexer.Common.Persistence.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Indexer.Common.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20200622142105_MoveObservedOperationToBlockchainSchema")]
    partial class MoveObservedOperationToBlockchainSchema
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("indexer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Indexer.Common.Persistence.Entities.FirstPassIndexers.FirstPassIndexerEntity", b =>
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

                    b.Property<DateTimeOffset>("StartedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("StepSize")
                        .HasColumnType("bigint");

                    b.Property<long>("StopBlock")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnName("xmin")
                        .HasColumnType("xid");

                    b.HasKey("Id");

                    b.HasIndex("BlockchainId")
                        .HasName("IX_FirstPassIndexers_BlockchainId");

                    b.ToTable("first_pass_indexers");
                });

            modelBuilder.Entity("Indexer.Common.Persistence.Entities.OngoingIndexers.OngoingIndexerEntity", b =>
                {
                    b.Property<string>("BlockchainId")
                        .HasColumnType("text");

                    b.Property<long>("NextBlock")
                        .HasColumnType("bigint");

                    b.Property<long>("Sequence")
                        .HasColumnType("bigint");

                    b.Property<long>("StartBlock")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("StartedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnName("xmin")
                        .HasColumnType("xid");

                    b.HasKey("BlockchainId");

                    b.ToTable("ongoing_indexers");
                });

            modelBuilder.Entity("Indexer.Common.Persistence.Entities.SecondPassIndexers.SecondPassIndexerEntity", b =>
                {
                    b.Property<string>("BlockchainId")
                        .HasColumnType("text");

                    b.Property<long>("NextBlock")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("StartedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<long>("StopBlock")
                        .HasColumnType("bigint");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<uint>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnName("xmin")
                        .HasColumnType("xid");

                    b.HasKey("BlockchainId");

                    b.ToTable("second_pass_indexers");
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
