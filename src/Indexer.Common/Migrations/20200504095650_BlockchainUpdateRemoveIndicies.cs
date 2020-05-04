using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class BlockchainUpdateRemoveIndicies : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Blockchain_Name",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropIndex(
                name: "IX_Blockchain_NetworkType",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropIndex(
                name: "IX_Blockchain_TenantId",
                schema: "indexer",
                table: "blockchains");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Blockchain_Name",
                schema: "indexer",
                table: "blockchains",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Blockchain_NetworkType",
                schema: "indexer",
                table: "blockchains",
                column: "NetworkType");

            migrationBuilder.CreateIndex(
                name: "IX_Blockchain_TenantId",
                schema: "indexer",
                table: "blockchains",
                column: "TenantId");
        }
    }
}
