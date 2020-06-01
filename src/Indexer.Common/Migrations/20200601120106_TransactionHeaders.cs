using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class TransactionHeaders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BlockHeaders_BlockchainId_Id",
                schema: "indexer",
                table: "block_headers");

            migrationBuilder.CreateTable(
                name: "transaction_headers",
                schema: "indexer",
                columns: table => new
                {
                    GlobalId = table.Column<string>(nullable: false),
                    BlockchainId = table.Column<string>(nullable: false),
                    BlockId = table.Column<string>(nullable: false),
                    Id = table.Column<string>(nullable: false),
                    Number = table.Column<int>(nullable: false),
                    ErrorMessage = table.Column<string>(nullable: true),
                    ErrorCode = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_headers", x => x.GlobalId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_headers",
                schema: "indexer");

            migrationBuilder.CreateIndex(
                name: "IX_BlockHeaders_BlockchainId_Id",
                schema: "indexer",
                table: "block_headers",
                columns: new[] { "BlockchainId", "Id" },
                unique: true);
        }
    }
}
