using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CarteraProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSprintEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename Estimation → EstimationHours in WorkItems
            migrationBuilder.RenameColumn(
                name: "Estimation",
                table: "WorkItems",
                newName: "EstimationHours");

            // Add new fields to WorkItems
            migrationBuilder.AddColumn<int>(
                name: "EstimationPoints",
                table: "WorkItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SprintId",
                table: "WorkItems",
                type: "integer",
                nullable: true);

            // Add estimation fields to Epics
            migrationBuilder.AddColumn<int>(
                name: "EstimationHours",
                table: "Epics",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimationPoints",
                table: "Epics",
                type: "integer",
                nullable: true);

            // Create Sprints table
            migrationBuilder.CreateTable(
                name: "Sprints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Goal = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sprints_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_SprintId",
                table: "WorkItems",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId",
                table: "Sprints",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItems_Sprints_SprintId",
                table: "WorkItems",
                column: "SprintId",
                principalTable: "Sprints",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkItems_Sprints_SprintId",
                table: "WorkItems");

            migrationBuilder.DropTable(
                name: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_SprintId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "EstimationPoints",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "SprintId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "EstimationHours",
                table: "Epics");

            migrationBuilder.DropColumn(
                name: "EstimationPoints",
                table: "Epics");

            migrationBuilder.RenameColumn(
                name: "EstimationHours",
                table: "WorkItems",
                newName: "Estimation");
        }
    }
}
