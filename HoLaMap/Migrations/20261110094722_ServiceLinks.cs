using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class ServiceLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FromServiceId = table.Column<int>(type: "integer", nullable: false),
                    ToServiceId = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceLinks_Services_FromServiceId",
                        column: x => x.FromServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceLinks_Services_ToServiceId",
                        column: x => x.ToServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLinks_FromServiceId_ToServiceId",
                table: "ServiceLinks",
                columns: new[] { "FromServiceId", "ToServiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceLinks_ToServiceId",
                table: "ServiceLinks",
                column: "ToServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceLinks");
        }
    }
}
