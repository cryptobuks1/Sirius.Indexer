using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Indexer.Common.Migrations
{
    public partial class Initial : Migration
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
                    Id = table.Column<string>(nullable: false),
                    Protocol = table.Column<string>(nullable: true),
                    TenantId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    NetworkType = table.Column<int>(nullable: false),
                    IntegrationUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blockchains", x => x.Id);
                });

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
                    StepSize = table.Column<long>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_first_pass_indexers", x => x.Id);
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
                    IsCompleted = table.Column<bool>(nullable: false),
                    BilV1OperationId = table.Column<Guid>(nullable: false),
                    AssetId = table.Column<long>(nullable: false),
                    Fees = table.Column<string>(nullable: true),
                    DestinationAddress = table.Column<string>(nullable: true),
                    OperationAmount = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observed_operations", x => x.OperationId);
                });

            migrationBuilder.CreateTable(
                name: "ongoing_indexers",
                schema: "indexer",
                columns: table => new
                {
                    BlockchainId = table.Column<string>(nullable: false),
                    StartBlock = table.Column<long>(nullable: false),
                    NextBlock = table.Column<long>(nullable: false),
                    Sequence = table.Column<long>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ongoing_indexers", x => x.BlockchainId);
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

            migrationBuilder.CreateTable(
                name: "second_pass_indexers",
                schema: "indexer",
                columns: table => new
                {
                    BlockchainId = table.Column<string>(nullable: false),
                    NextBlock = table.Column<long>(nullable: false),
                    StopBlock = table.Column<long>(nullable: false),
                    xmin = table.Column<uint>(type: "xid", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_second_pass_indexers", x => x.BlockchainId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirstPassIndexers_BlockchainId",
                schema: "indexer",
                table: "first_pass_indexers",
                column: "BlockchainId");

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
                name: "first_pass_indexers",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "observed_operations",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "ongoing_indexers",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "indexer");

            migrationBuilder.DropTable(
                name: "second_pass_indexers",
                schema: "indexer");
        }
    }
}
