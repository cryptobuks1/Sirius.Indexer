using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class AssetsIndexesFixed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_assets_symbol",
                schema: "indexer",
                table: "assets");

            migrationBuilder.DropIndex(
                name: "ix_assets_symbol_address",
                schema: "indexer",
                table: "assets");

            migrationBuilder.CreateIndex(
                name: "ix_assets_blockchainId_symbol",
                schema: "indexer",
                table: "assets",
                columns: new[] { "BlockchainId", "Symbol" },
                unique: true,
                filter: "\"Address\" is null");

            migrationBuilder.CreateIndex(
                name: "ix_assets_blockchainId_symbol_address",
                schema: "indexer",
                table: "assets",
                columns: new[] { "BlockchainId", "Symbol", "Address" },
                unique: true,
                filter: "\"Address\" is not null");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_assets_blockchainId_symbol",
                schema: "indexer",
                table: "assets");

            migrationBuilder.DropIndex(
                name: "ix_assets_blockchainId_symbol_address",
                schema: "indexer",
                table: "assets");

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
        }
    }
}
