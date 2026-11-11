using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolarUY.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenancePanelOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ResolutionNotes",
                table: "MaintenanceOrders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PanelOutcome",
                table: "MaintenanceOrders",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PanelOutcome",
                table: "MaintenanceOrders");

            migrationBuilder.AlterColumn<string>(
                name: "ResolutionNotes",
                table: "MaintenanceOrders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
