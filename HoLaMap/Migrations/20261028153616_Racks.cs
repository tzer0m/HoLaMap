using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class Racks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RackColumn",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RackHeight",
                table: "Devices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RackId",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RackU",
                table: "Devices",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RackWidth",
                table: "Devices",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Racks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoomId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    HeightU = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Racks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Racks_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_RackId",
                table: "Devices",
                column: "RackId");

            migrationBuilder.CreateIndex(
                name: "IX_Racks_RoomId_Name",
                table: "Racks",
                columns: new[] { "RoomId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_Racks_RackId",
                table: "Devices",
                column: "RackId",
                principalTable: "Racks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_Racks_RackId",
                table: "Devices");

            migrationBuilder.DropTable(
                name: "Racks");

            migrationBuilder.DropIndex(
                name: "IX_Devices_RackId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RackColumn",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RackHeight",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RackId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RackU",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "RackWidth",
                table: "Devices");
        }
    }
}
