using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddInverterMinuteReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InverterMinuteReadings",
                columns: table => new
                {
                    InverterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinuteUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SolarParkId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolarNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvgPowerKw = table.Column<double>(type: "double precision", nullable: false),
                    MaxPowerKw = table.Column<double>(type: "double precision", nullable: false),
                    EnergyKwh = table.Column<double>(type: "double precision", nullable: false),
                    AvgDcVoltageV = table.Column<double>(type: "double precision", nullable: false),
                    AvgDcCurrentA = table.Column<double>(type: "double precision", nullable: false),
                    AvgAcVoltageV = table.Column<double>(type: "double precision", nullable: false),
                    AvgFrequencyHz = table.Column<double>(type: "double precision", nullable: false),
                    AvgTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    MaxTemperatureC = table.Column<double>(type: "double precision", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    AnomalousSamples = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InverterMinuteReadings", x => new { x.InverterId, x.MinuteUtc });
                });

            migrationBuilder.CreateIndex(
                name: "IX_InverterMinuteReadings_SolarNodeId_MinuteUtc",
                table: "InverterMinuteReadings",
                columns: new[] { "SolarNodeId", "MinuteUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InverterMinuteReadings_SolarParkId_MinuteUtc",
                table: "InverterMinuteReadings",
                columns: new[] { "SolarParkId", "MinuteUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InverterMinuteReadings");
        }
    }
}
