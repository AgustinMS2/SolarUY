using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddParkHourlyGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkHourlyGenerations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SolarParkId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    HourLocal = table.Column<int>(type: "integer", nullable: false),
                    EnergyGeneratedKwh = table.Column<double>(type: "double precision", nullable: false),
                    PeakPowerKw = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkHourlyGenerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkHourlyGenerations_SolarParks_SolarParkId",
                        column: x => x.SolarParkId,
                        principalTable: "SolarParks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkHourlyGenerations_SolarParkId_DateLocal",
                table: "ParkHourlyGenerations",
                columns: new[] { "SolarParkId", "DateLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkHourlyGenerations_SolarParkId_DateLocal_HourLocal",
                table: "ParkHourlyGenerations",
                columns: new[] { "SolarParkId", "DateLocal", "HourLocal" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkHourlyGenerations");
        }
    }
}
