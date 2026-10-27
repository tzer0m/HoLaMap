using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class PoEPower : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PoEPower",
                table: "Ports",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoEPower",
                table: "Ports");
        }
    }
}
