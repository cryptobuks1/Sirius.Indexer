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
    [Migration("20200504095650_BlockchainUpdateRemoveIndicies")]
    partial class BlockchainUpdateRemoveIndicies
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("indexer")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

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

            modelBuilder.Entity("Indexer.Common.ReadModel.Blockchains.Blockchain", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("IntegrationUrl")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<int>("NetworkType")
                        .HasColumnType("integer");

                    b.Property<long>("StartBlockNumber")
                        .HasColumnType("bigint");

                    b.Property<string>("TenantId")
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

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

            modelBuilder.Entity("Indexer.Common.ReadModel.Blockchains.Blockchain", b =>
                {
                    b.OwnsOne("Indexer.Common.ReadModel.Blockchains.Protocol", "Protocol", b1 =>
                        {
                            b1.Property<string>("BlockchainId")
                                .HasColumnType("text");

                            b1.Property<string>("Code")
                                .HasColumnName("ProtocolCode")
                                .HasColumnType("text");

                            b1.Property<int>("DoubleSpendingProtectionType")
                                .HasColumnName("DoubleSpendingProtectionType")
                                .HasColumnType("integer");

                            b1.Property<string>("Name")
                                .HasColumnName("ProtocolName")
                                .HasColumnType("text");

                            b1.HasKey("BlockchainId");

                            b1.ToTable("blockchains");

                            b1.WithOwner()
                                .HasForeignKey("BlockchainId");

                            b1.OwnsOne("Indexer.Common.ReadModel.Blockchains.Capabilities", "Capabilities", b2 =>
                                {
                                    b2.Property<string>("ProtocolBlockchainId")
                                        .HasColumnType("text");

                                    b2.HasKey("ProtocolBlockchainId");

                                    b2.ToTable("blockchains");

                                    b2.WithOwner()
                                        .HasForeignKey("ProtocolBlockchainId");

                                    b2.OwnsOne("Indexer.Common.ReadModel.Blockchains.DestinationTagCapabilities", "DestinationTag", b3 =>
                                        {
                                            b3.Property<string>("CapabilitiesProtocolBlockchainId")
                                                .HasColumnType("text");

                                            b3.HasKey("CapabilitiesProtocolBlockchainId");

                                            b3.ToTable("blockchains");

                                            b3.WithOwner()
                                                .HasForeignKey("CapabilitiesProtocolBlockchainId");

                                            b3.OwnsOne("Indexer.Common.ReadModel.Blockchains.NumberDestinationTagsCapabilities", "Number", b4 =>
                                                {
                                                    b4.Property<string>("DestinationTagCapabilitiesCapabilitiesProtocolBlockchainId")
                                                        .HasColumnType("text");

                                                    b4.Property<long>("Max")
                                                        .HasColumnType("bigint");

                                                    b4.Property<long>("Min")
                                                        .HasColumnType("bigint");

                                                    b4.Property<string>("Name")
                                                        .HasColumnType("text");

                                                    b4.HasKey("DestinationTagCapabilitiesCapabilitiesProtocolBlockchainId");

                                                    b4.ToTable("blockchains");

                                                    b4.WithOwner()
                                                        .HasForeignKey("DestinationTagCapabilitiesCapabilitiesProtocolBlockchainId");
                                                });

                                            b3.OwnsOne("Indexer.Common.ReadModel.Blockchains.TextDestinationTagsCapabilities", "Text", b4 =>
                                                {
                                                    b4.Property<string>("DestinationTagCapabilitiesCapabilitiesProtocolBlockchainId")
                                                        .HasColumnType("text");

                                                    b4.Property<long>("MaxLength")
                                                        .HasColumnType("bigint");

                                                    b4.Property<string>("Name")
                                                        .HasColumnType("text");

                                                    b4.HasKey("DestinationTagCapabilitiesCapabilitiesProtocolBlockchainId");

                                                    b4.ToTable("blockchains");

                                                    b4.WithOwner()
                                                        .HasForeignKey("DestinationTagCapabilitiesCapabilitiesProtocolBlockchainId");
                                                });
                                        });
                                });

                            b1.OwnsOne("Indexer.Common.ReadModel.Blockchains.Requirements", "Requirements", b2 =>
                                {
                                    b2.Property<string>("ProtocolBlockchainId")
                                        .HasColumnType("text");

                                    b2.Property<bool>("PublicKey")
                                        .HasColumnType("boolean");

                                    b2.HasKey("ProtocolBlockchainId");

                                    b2.ToTable("blockchains");

                                    b2.WithOwner()
                                        .HasForeignKey("ProtocolBlockchainId");
                                });
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
