using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarteraProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorWorkItemAssigneesAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkItems_Persons_AssignedToId",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_AssignedToId",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "WorkItems");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "WorkItems",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkItemAssignments",
                columns: table => new
                {
                    AssigneesId = table.Column<int>(type: "integer", nullable: false),
                    WorkItemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemAssignments", x => new { x.AssigneesId, x.WorkItemId });
                    table.ForeignKey(
                        name: "FK_WorkItemAssignments_Persons_AssigneesId",
                        column: x => x.AssigneesId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkItemAssignments_WorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemAssignments_WorkItemId",
                table: "WorkItemAssignments",
                column: "WorkItemId");

            // Rename enum value: InReview → Blocked
            migrationBuilder.Sql("UPDATE \"WorkItems\" SET \"Status\" = 'Blocked' WHERE \"Status\" = 'InReview';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkItemAssignments");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "WorkItems");

            migrationBuilder.AddColumn<int>(
                name: "AssignedToId",
                table: "WorkItems",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_AssignedToId",
                table: "WorkItems",
                column: "AssignedToId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkItems_Persons_AssignedToId",
                table: "WorkItems",
                column: "AssignedToId",
                principalTable: "Persons",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
