using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Indexer.Common.Migrations
{
    public partial class MoveObservedOperationToBlockchainSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "observed_operations",
                schema: "indexer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "observed_operations",
                schema: "indexer",
                columns: table => new
                {
                    OperationId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssetId = table.Column<long>(type: "bigint", nullable: false),
                    BilV1OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockchainId = table.Column<string>(type: "text", nullable: true),
                    DestinationAddress = table.Column<string>(type: "text", nullable: true),
                    Fees = table.Column<string>(type: "text", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    OperationAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observed_operations", x => x.OperationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObservedOperations_IsCompleted",
                schema: "indexer",
                table: "observed_operations",
                column: "IsCompleted");
        }
    }
}
