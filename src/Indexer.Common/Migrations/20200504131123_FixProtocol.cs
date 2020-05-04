using Microsoft.EntityFrameworkCore.Migrations;

namespace Indexer.Common.Migrations
{
    public partial class FixProtocol : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "Protocol",
                schema: "indexer",
                table: "blockchains",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Protocol",
                schema: "indexer",
                table: "blockchains");

            migrationBuilder.AddColumn<long>(
                name: "Protocol_Capabilities_DestinationTag_Number_Max",
                schema: "indexer",
                table: "blockchains",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Protocol_Capabilities_DestinationTag_Number_Min",
                schema: "indexer",
                table: "blockchains",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Protocol_Capabilities_DestinationTag_Number_Name",
                schema: "indexer",
                table: "blockchains",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtocolCode",
                schema: "indexer",
                table: "blockchains",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DoubleSpendingProtectionType",
                schema: "indexer",
                table: "blockchains",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtocolName",
                schema: "indexer",
                table: "blockchains",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Protocol_Requirements_PublicKey",
                schema: "indexer",
                table: "blockchains",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Protocol_Capabilities_DestinationTag_Text_MaxLength",
                schema: "indexer",
                table: "blockchains",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Protocol_Capabilities_DestinationTag_Text_Name",
                schema: "indexer",
                table: "blockchains",
                type: "text",
                nullable: true);
        }
    }
}
