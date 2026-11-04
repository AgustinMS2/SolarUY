using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddInverterEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inverters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SerieNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedSerieNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BrandAndModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NominalCapacityKw = table.Column<double>(type: "double precision", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SolarNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inverters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inverters_SolarNodes_SolarNodeId",
                        column: x => x.SolarNodeId,
                        principalTable: "SolarNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inverters_NormalizedSerieNumber",
                table: "Inverters",
                column: "NormalizedSerieNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inverters_SolarNodeId",
                table: "Inverters",
                column: "SolarNodeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inverters");
        }
    }
}
