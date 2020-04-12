using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Indexer.Bilv1.Repositories.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "indexer_bil_v1");

            migrationBuilder.CreateTable(
                name: "enrolled_balance",
                schema: "indexer_bil_v1",
                columns: table => new
                {
                    BlockchianId = table.Column<string>(nullable: false),
                    BlockchainAssetId = table.Column<string>(nullable: false),
                    WalletAddress = table.Column<string>(nullable: false),
                    OriginalWalletAddress = table.Column<string>(nullable: true),
                    BlockNumber = table.Column<long>(nullable: false),
                    Balance = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrolled_balance", x => new { x.BlockchianId, x.BlockchainAssetId, x.WalletAddress });
                });

            migrationBuilder.CreateTable(
                name: "operations",
                schema: "indexer_bil_v1",
                columns: table => new
                {
                    BlockchianId = table.Column<string>(nullable: false),
                    BlockchainAssetId = table.Column<string>(nullable: false),
                    WalletAddress = table.Column<string>(nullable: false),
                    OperationId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OriginalWalletAddress = table.Column<string>(nullable: true),
                    BlockNumber = table.Column<long>(nullable: false),
                    BalanceChange = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operations", x => new { x.BlockchianId, x.BlockchainAssetId, x.WalletAddress, x.OperationId });
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                schema: "indexer_bil_v1",
                columns: table => new
                {
                    BlockchainId = table.Column<string>(nullable: false),
                    NetworkId = table.Column<string>(nullable: false),
                    Id = table.Column<Guid>(nullable: false),
                    WalletAddress = table.Column<string>(nullable: true),
                    OriginalWalletAddress = table.Column<string>(nullable: true),
                    PublicKey = table.Column<string>(nullable: true),
                    IsCompromised = table.Column<bool>(nullable: false),
                    ImportedDateTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => new { x.BlockchainId, x.NetworkId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockchainId_NetworkId_WalletAddress",
                schema: "indexer_bil_v1",
                table: "wallets",
                columns: new[] { "BlockchainId", "NetworkId", "WalletAddress" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enrolled_balance",
                schema: "indexer_bil_v1");

            migrationBuilder.DropTable(
                name: "operations",
                schema: "indexer_bil_v1");

            migrationBuilder.DropTable(
                name: "wallets",
                schema: "indexer_bil_v1");
        }
    }
}
