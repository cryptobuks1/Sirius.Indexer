using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Indexer.Common.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "indexer");

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "indexer",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'100000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BlockchainId = table.Column<string>(nullable: true),
                    Symbol = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Accuracy = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "blockchains",
                schema: "indexer",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Protocol = table.Column<string>(nullable: true),
                    TenantId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    NetworkType = table.Column<int>(nullable: false),
                    IntegrationUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blockchains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "first_pass_indexers",
                schema: "indexer",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    BlockchainId = table.Column<string>(nullable: false),
                    StartBlock = table.Column<long>(nullable: false),
                    StopBlock = table.Column<long>(nullable: false),
                    NextBlock = table.Column<long>(nullable: false),
                    StepSize = table.Column<long>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_first_pass_indexers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ongoing_indexers",
                schema: "indexer",
                columns: table => new
                {
                    BlockchainId = table.Column<string>(nullable: false),
                    StartBlock = table.Column<long>(nullable: false),
                    NextBlock = table.Column<long>(nullable: false),
                    Sequence = table.Column<long>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ongoing_indexers", x => x.BlockchainId);
                });

            migrationBuilder.CreateTable(
                name: "second_pass_indexers",
                schema: "indexer",
                columns: table => new
                {
                    BlockchainId = table.Column<string>(nullable: false),
                    NextBlock = table.Column<long>(nullable: false),
                    StopBlock = table.Column<long>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_second_pass_indexers", x => x.BlockchainId);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assets_blockchain_id",
                schema: "indexer",
                table: "assets",
                column: "BlockchainId");

            migrationBuilder.CreateIndex(
                name: "ix_assets_symbol",
                schema: "indexer",
                table: "assets",
                column: "Symbol",
                unique: true,
                filter: "\"Address\" is null");

            migrationBuilder.CreateIndex(
                name: "ix_assets_symbol_address",
                schema: "indexer",
                table: "assets",
                columns: new[] { "Symbol", "Address" },
                unique: true,
                filter: "\"Address\" is not null");

            migrationBuilder.CreateIndex(
                name: "IX_FirstPassIndexers_BlockchainId",
                schema: "indexer",
                table: "first_pass_indexers",
                column: "BlockchainId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assets",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "blockchains",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "first_pass_indexers",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "ongoing_indexers",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "second_pass_indexers",
                schema: "indexer");
        }
    }
}
