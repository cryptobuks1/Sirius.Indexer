using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class FirstPassIndexers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    xmin = table.Column<uint>(type: "xid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_first_pass_indexers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirstPassIndexers_Blockchain",
                schema: "indexer",
                table: "first_pass_indexers",
                column: "BlockchainId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "first_pass_indexers",
                schema: "indexer");
        }
    }
}
