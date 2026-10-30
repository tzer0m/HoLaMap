using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class PatchPanels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "RackPanels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PortCount",
                table: "RackPanels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PatchConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RackPanelId = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    PortId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatchConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatchConnections_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatchConnections_Ports_PortId",
                        column: x => x.PortId,
                        principalTable: "Ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatchConnections_RackPanels_RackPanelId",
                        column: x => x.RackPanelId,
                        principalTable: "RackPanels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatchConnections_DeviceId",
                table: "PatchConnections",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PatchConnections_PortId",
                table: "PatchConnections",
                column: "PortId");

            migrationBuilder.CreateIndex(
                name: "IX_PatchConnections_RackPanelId_Number_Side",
                table: "PatchConnections",
                columns: new[] { "RackPanelId", "Number", "Side" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatchConnections");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "RackPanels");

            migrationBuilder.DropColumn(
                name: "PortCount",
                table: "RackPanels");
        }
    }
}
