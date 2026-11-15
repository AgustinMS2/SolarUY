using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddGridInjectionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GridInjectionEnabled",
                table: "SolarParks",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "GridInjectionEnabled",
                table: "Inverters",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "GridInjectionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ParkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    OrderedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    OrderedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemoteIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OrdersSent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GridInjectionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GridInjectionEvents_SolarParks_ParkId",
                        column: x => x.ParkId,
                        principalTable: "SolarParks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GridInjectionEvents_ParkId_OrderedAtUtc",
                table: "GridInjectionEvents",
                columns: new[] { "ParkId", "OrderedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GridInjectionEvents");

            migrationBuilder.DropColumn(
                name: "GridInjectionEnabled",
                table: "SolarParks");

            migrationBuilder.DropColumn(
                name: "GridInjectionEnabled",
                table: "Inverters");
        }
    }
}
