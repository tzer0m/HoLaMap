using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class ServiceMine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Mine",
                table: "Services",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mine",
                table: "Services");
        }
    }
}
