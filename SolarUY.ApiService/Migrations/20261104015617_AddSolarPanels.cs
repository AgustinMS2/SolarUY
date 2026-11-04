using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddSolarPanels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolarPanels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SerieNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedSerieNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PowerW = table.Column<int>(type: "integer", nullable: false),
                    Efficiency = table.Column<double>(type: "double precision", nullable: false),
                    SolarParkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolarPanels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolarPanels_SolarParks_SolarParkId",
                        column: x => x.SolarParkId,
                        principalTable: "SolarParks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolarPanels_NormalizedSerieNumber",
                table: "SolarPanels",
                column: "NormalizedSerieNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolarPanels_SolarParkId",
                table: "SolarPanels",
                column: "SolarParkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolarPanels");
        }
    }
}
