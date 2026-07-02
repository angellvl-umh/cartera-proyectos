using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarteraProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintPointsSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommittedPoints",
                table: "Sprints",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveredPoints",
                table: "Sprints",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommittedPoints",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "DeliveredPoints",
                table: "Sprints");
        }
    }
}
