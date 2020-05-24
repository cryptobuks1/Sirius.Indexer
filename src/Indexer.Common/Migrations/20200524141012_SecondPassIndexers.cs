using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class SecondPassIndexers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "second_pass_indexers",
                schema: "indexer",
                columns: table => new
                {
                    BlockchainId = table.Column<string>(nullable: false),
                    NextBlock = table.Column<long>(nullable: false),
                    StopBlock = table.Column<long>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_second_pass_indexers", x => x.BlockchainId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "second_pass_indexers",
                schema: "indexer");
        }
    }
}
