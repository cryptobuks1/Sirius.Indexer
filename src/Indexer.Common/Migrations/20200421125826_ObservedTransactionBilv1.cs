using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class ObservedTransactionBilv1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AssetId",
                schema: "indexer",
                table: "observed_operations",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "BilV1OperationId",
                schema: "indexer",
                table: "observed_operations",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DestinationAddress",
                schema: "indexer",
                table: "observed_operations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fees",
                schema: "indexer",
                table: "observed_operations",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OperationAmount",
                schema: "indexer",
                table: "observed_operations",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetId",
                schema: "indexer",
                table: "observed_operations");

            migrationBuilder.DropColumn(
                name: "BilV1OperationId",
                schema: "indexer",
                table: "observed_operations");

            migrationBuilder.DropColumn(
                name: "DestinationAddress",
                schema: "indexer",
                table: "observed_operations");

            migrationBuilder.DropColumn(
                name: "Fees",
                schema: "indexer",
                table: "observed_operations");

            migrationBuilder.DropColumn(
                name: "OperationAmount",
                schema: "indexer",
                table: "observed_operations");
        }
    }
}
