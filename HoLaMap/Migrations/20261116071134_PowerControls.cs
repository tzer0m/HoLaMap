using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class PowerControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanSleep",
                table: "Devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PowerCheckPort",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PowerControl",
                table: "Devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SemaphoreHost",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SemaphoreInventoryId",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WakeInterface",
                table: "Devices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WakeMac",
                table: "Devices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanSleep",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PowerCheckPort",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "PowerControl",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "SemaphoreHost",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "SemaphoreInventoryId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WakeInterface",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "WakeMac",
                table: "Devices");
        }
    }
}
