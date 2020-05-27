using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class ImprovedIndexerInfo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                schema: "indexer",
                table: "second_pass_indexers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "indexer",
                table: "second_pass_indexers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "StartBlock",
                schema: "indexer",
                table: "ongoing_indexers",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                schema: "indexer",
                table: "ongoing_indexers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "indexer",
                table: "ongoing_indexers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartedAt",
                schema: "indexer",
                table: "first_pass_indexers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "StepSize",
                schema: "indexer",
                table: "first_pass_indexers",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "indexer",
                table: "first_pass_indexers",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "indexer",
                table: "second_pass_indexers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "indexer",
                table: "second_pass_indexers");

            migrationBuilder.DropColumn(
                name: "StartBlock",
                schema: "indexer",
                table: "ongoing_indexers");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "indexer",
                table: "ongoing_indexers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "indexer",
                table: "ongoing_indexers");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                schema: "indexer",
                table: "first_pass_indexers");

            migrationBuilder.DropColumn(
                name: "StepSize",
                schema: "indexer",
                table: "first_pass_indexers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "indexer",
                table: "first_pass_indexers");
        }
    }
}
