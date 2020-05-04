using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class BlockchainUpdate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_blockchains",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "BlockchainId",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                schema: "indexer",
                table: "blockchains",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "indexer",
                table: "blockchains",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NetworkType",
                schema: "indexer",
                table: "blockchains",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "StartBlockNumber",
                schema: "indexer",
                table: "blockchains",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "indexer",
                table: "blockchains",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "Protocol_Capabilities_DestinationTag_Number_Max",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Protocol_Capabilities_DestinationTag_Number_Min",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Protocol_Capabilities_DestinationTag_Number_Name",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtocolCode",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoubleSpendingProtectionType",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtocolName",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Protocol_Requirements_PublicKey",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Protocol_Capabilities_DestinationTag_Text_MaxLength",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Protocol_Capabilities_DestinationTag_Text_Name",
                schema: "indexer",
                table: "blockchains",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_blockchains",
                schema: "indexer",
                table: "blockchains",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Blockchain_Name",
                schema: "indexer",
                table: "blockchains",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Blockchain_NetworkType",
                schema: "indexer",
                table: "blockchains",
                column: "NetworkType");

            migrationBuilder.CreateIndex(
                name: "IX_Blockchain_TenantId",
                schema: "indexer",
                table: "blockchains",
                column: "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_blockchains",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropIndex(
                name: "IX_Blockchain_Name",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropIndex(
                name: "IX_Blockchain_NetworkType",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropIndex(
                name: "IX_Blockchain_TenantId",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "NetworkType",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "StartBlockNumber",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "Protocol_Capabilities_DestinationTag_Number_Max",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "Protocol_Capabilities_DestinationTag_Number_Min",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "Protocol_Capabilities_DestinationTag_Number_Name",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "ProtocolCode",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "DoubleSpendingProtectionType",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "ProtocolName",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "Protocol_Requirements_PublicKey",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "Protocol_Capabilities_DestinationTag_Text_MaxLength",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.DropColumn(
                name: "Protocol_Capabilities_DestinationTag_Text_Name",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.AddColumn<string>(
                name: "BlockchainId",
                schema: "indexer",
                table: "blockchains",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_blockchains",
                schema: "indexer",
                table: "blockchains",
                column: "BlockchainId");
        }
    }
}
