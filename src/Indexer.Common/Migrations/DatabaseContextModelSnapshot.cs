﻿// <auto-generated />
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

            modelBuilder.Entity("Indexer.Common.ReadModel.Blockchains.Blockchain", b =>
                {
                    b.Property<string>("BlockchainId")
                        .HasColumnType("text");

                    b.Property<string>("IntegrationUrl")
                        .HasColumnType("text");

                    b.HasKey("BlockchainId");

                    b.ToTable("blockchains");
                });
#pragma warning restore 612, 618
        }
    }
}
