using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class BlocksRenamedToBlockHeaders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocks",
                schema: "indexer");

            migrationBuilder.CreateTable(
                name: "block_headers",
                schema: "indexer",
                columns: table => new
                {
                    GlobalId = table.Column<string>(nullable: false),
                    BlockchainId = table.Column<string>(nullable: false),
                    Id = table.Column<string>(nullable: false),
                    Number = table.Column<long>(nullable: false),
                    PreviousId = table.Column<string>(nullable: true),
                    MinedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_block_headers", x => x.GlobalId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockHeaders_BlockchainId_Id",
                schema: "indexer",
                table: "block_headers",
                columns: new[] { "BlockchainId", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_BlockchainId_Number",
                schema: "indexer",
                table: "block_headers",
                columns: new[] { "BlockchainId", "Number" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "block_headers",
                schema: "indexer");

            migrationBuilder.CreateTable(
                name: "blocks",
                schema: "indexer",
                columns: table => new
                {
                    GlobalId = table.Column<string>(type: "text", nullable: false),
                    BlockchainId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<long>(type: "bigint", nullable: false),
                    PreviousId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocks", x => x.GlobalId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_Blockchain_Id",
                schema: "indexer",
                table: "blocks",
                columns: new[] { "BlockchainId", "Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_Blockchain_Number",
                schema: "indexer",
                table: "blocks",
                columns: new[] { "BlockchainId", "Number" },
                unique: true);
        }
    }
}
