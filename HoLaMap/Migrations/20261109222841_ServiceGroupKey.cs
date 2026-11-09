using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class ServiceGroupKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_DeviceId_Kind_Name",
                table: "Services");

            migrationBuilder.CreateIndex(
                name: "IX_Services_DeviceId_Kind_Group_Name",
                table: "Services",
                columns: new[] { "DeviceId", "Kind", "Group", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Services_DeviceId_Kind_Group_Name",
                table: "Services");

            migrationBuilder.CreateIndex(
                name: "IX_Services_DeviceId_Kind_Name",
                table: "Services",
                columns: new[] { "DeviceId", "Kind", "Name" },
                unique: true);
        }
    }
}
