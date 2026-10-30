using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class PoeSplitters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PoESplitter",
                table: "Links",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PoESplitter",
                table: "Links");
        }
    }
}
