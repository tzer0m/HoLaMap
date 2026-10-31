using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class ZigbeeDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ZigbeeDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Ieee = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: false),
                    PowerSource = table.Column<string>(type: "text", nullable: true),
                    Lqi = table.Column<int>(type: "integer", nullable: true),
                    Rssi = table.Column<int>(type: "integer", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Available = table.Column<bool>(type: "boolean", nullable: false),
                    RoomId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZigbeeDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZigbeeDevices_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ZigbeeNeighbors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    NeighborIeee = table.Column<string>(type: "text", nullable: false),
                    Lqi = table.Column<int>(type: "integer", nullable: true),
                    Relationship = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZigbeeNeighbors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZigbeeNeighbors_ZigbeeDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "ZigbeeDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ZigbeeDevices_Ieee",
                table: "ZigbeeDevices",
                column: "Ieee",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZigbeeDevices_RoomId",
                table: "ZigbeeDevices",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_ZigbeeNeighbors_DeviceId",
                table: "ZigbeeNeighbors",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ZigbeeNeighbors");

            migrationBuilder.DropTable(
                name: "ZigbeeDevices");
        }
    }
}
