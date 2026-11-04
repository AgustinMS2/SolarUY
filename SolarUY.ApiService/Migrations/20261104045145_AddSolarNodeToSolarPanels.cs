using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddSolarNodeToSolarPanels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SolarNodeId",
                table: "SolarPanels",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolarPanels_SolarNodeId",
                table: "SolarPanels",
                column: "SolarNodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_SolarPanels_SolarNodes_SolarNodeId",
                table: "SolarPanels",
                column: "SolarNodeId",
                principalTable: "SolarNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolarPanels_SolarNodes_SolarNodeId",
                table: "SolarPanels");

            migrationBuilder.DropIndex(
                name: "IX_SolarPanels_SolarNodeId",
                table: "SolarPanels");

            migrationBuilder.DropColumn(
                name: "SolarNodeId",
                table: "SolarPanels");
        }
    }
}
