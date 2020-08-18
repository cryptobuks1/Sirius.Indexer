using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class UnifiedMessageProcessing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "indexer",
                table: "blockchains",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "indexer",
                table: "blockchains",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "indexer",
                table: "blockchains");
        }
    }
}
