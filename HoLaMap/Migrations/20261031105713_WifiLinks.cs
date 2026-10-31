using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class WifiLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ssid",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WifiBand",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WifiChannel",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WifiRate",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WifiSignal",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WirelessApId",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_WirelessApId",
                table: "Devices",
                column: "WirelessApId");

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Devices_WirelessApId",
                table: "Devices",
                column: "WirelessApId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Devices_WirelessApId",
                table: "Devices");

            migrationBuilder.DropIndex(
                name: "IX_Devices_WirelessApId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Ssid",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WifiBand",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WifiChannel",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WifiRate",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WifiSignal",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WirelessApId",
                table: "Devices");
        }
    }
}
