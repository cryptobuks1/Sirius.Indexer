using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class TransactionHeadersIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TransactionHeaders_BlockchainId_BlockId",
                schema: "indexer",
                table: "transaction_headers",
                columns: new[] { "BlockchainId", "BlockId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransactionHeaders_BlockchainId_BlockId",
                schema: "indexer",
                table: "transaction_headers");
        }
    }
}
