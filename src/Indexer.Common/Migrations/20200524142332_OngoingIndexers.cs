using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class OngoingIndexers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ongoing_indexers",
                schema: "indexer",
                columns: table => new
                {
                    BlockchainId = table.Column<string>(nullable: false),
                    NextBlock = table.Column<long>(nullable: false),
                    Sequence = table.Column<long>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ongoing_indexers", x => x.BlockchainId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ongoing_indexers",
                schema: "indexer");
        }
    }
}
