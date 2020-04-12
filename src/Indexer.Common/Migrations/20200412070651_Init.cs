using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "indexer");

            migrationBuilder.CreateTable(
                name: "blockchains",
                schema: "indexer",
                columns: table => new
                {
                    BlockchainId = table.Column<string>(nullable: false),
                    IntegrationUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blockchains", x => x.BlockchainId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blockchains",
                schema: "indexer");
        }
    }
}
