using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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

            migrationBuilder.CreateTable(
                name: "observed_operations",
                schema: "indexer",
                columns: table => new
                {
                    OperationId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BlockchainId = table.Column<string>(nullable: true),
                    TransactionId = table.Column<string>(nullable: true),
                    IsCompleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observed_operations", x => x.OperationId);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "indexer",
                columns: table => new
                {
                    RequestId = table.Column<string>(nullable: false),
                    AggregateId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'2', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Response = table.Column<string>(nullable: true),
                    Events = table.Column<string>(nullable: true),
                    Commands = table.Column<string>(nullable: true),
                    IsStored = table.Column<bool>(nullable: false),
                    IsDispatched = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.RequestId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObservedOperations_IsCompleted",
                schema: "indexer",
                table: "observed_operations",
                column: "IsCompleted");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blockchains",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "observed_operations",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "indexer");
        }
    }
}
