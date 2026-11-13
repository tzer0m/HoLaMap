using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HoLaMap.Migrations
{
    /// <inheritdoc />
    public partial class CloudflareTunnels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<int>>(
                name: "Ports",
                table: "Services",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.CreateTable(
                name: "Tunnels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CloudflareId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: true),
                    RemoteConfig = table.Column<bool>(type: "boolean", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tunnels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tunnels_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TunnelRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TunnelId = table.Column<int>(type: "integer", nullable: false),
                    Hostname = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    ServiceId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TunnelRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TunnelRoutes_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TunnelRoutes_Tunnels_TunnelId",
                        column: x => x.TunnelId,
                        principalTable: "Tunnels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TunnelRoutes_ServiceId",
                table: "TunnelRoutes",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_TunnelRoutes_TunnelId_Hostname_Path",
                table: "TunnelRoutes",
                columns: new[] { "TunnelId", "Hostname", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tunnels_CloudflareId",
                table: "Tunnels",
                column: "CloudflareId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tunnels_DeviceId",
                table: "Tunnels",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TunnelRoutes");

            migrationBuilder.DropTable(
                name: "Tunnels");

            migrationBuilder.DropColumn(
                name: "Ports",
                table: "Services");
        }
    }
}
