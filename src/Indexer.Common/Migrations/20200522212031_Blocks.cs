using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class Blocks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "blocks",
                schema: "indexer",
                columns: table => new
                {
                    GlobalId = table.Column<string>(nullable: false),
                    BlockchainId = table.Column<string>(nullable: false),
                    Id = table.Column<string>(nullable: false),
                    Number = table.Column<long>(nullable: false),
                    PreviousId = table.Column<string>(nullable: true)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blocks",
                schema: "indexer");
        }
    }
}
