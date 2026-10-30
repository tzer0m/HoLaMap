using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class PatchJumpers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DeviceId",
                table: "PatchConnections",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "TargetNumber",
                table: "PatchConnections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetPanelId",
                table: "PatchConnections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetSide",
                table: "PatchConnections",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatchConnections_TargetPanelId",
                table: "PatchConnections",
                column: "TargetPanelId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatchConnections_RackPanels_TargetPanelId",
                table: "PatchConnections",
                column: "TargetPanelId",
                principalTable: "RackPanels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatchConnections_RackPanels_TargetPanelId",
                table: "PatchConnections");

            migrationBuilder.DropIndex(
                name: "IX_PatchConnections_TargetPanelId",
                table: "PatchConnections");

            migrationBuilder.DropColumn(
                name: "TargetNumber",
                table: "PatchConnections");

            migrationBuilder.DropColumn(
                name: "TargetPanelId",
                table: "PatchConnections");

            migrationBuilder.DropColumn(
                name: "TargetSide",
                table: "PatchConnections");

            migrationBuilder.AlterColumn<int>(
                name: "DeviceId",
                table: "PatchConnections",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
