using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class MultipleLinksPerPort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Links_FromPortId",
                table: "Links");

            migrationBuilder.CreateIndex(
                name: "IX_Links_FromPortId",
                table: "Links",
                column: "FromPortId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Links_FromPortId",
                table: "Links");

            migrationBuilder.CreateIndex(
                name: "IX_Links_FromPortId",
                table: "Links",
                column: "FromPortId",
                unique: true);
        }
    }
}
