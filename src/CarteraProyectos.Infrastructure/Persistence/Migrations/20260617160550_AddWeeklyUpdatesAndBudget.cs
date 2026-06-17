using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CarteraProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyUpdatesAndBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedBudget",
                table: "Projects",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectWeeklyUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    WeekOf = table.Column<DateOnly>(type: "date", nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    HealthStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectWeeklyUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectWeeklyUpdates_Persons_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectWeeklyUpdates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWeeklyUpdates_AuthorId",
                table: "ProjectWeeklyUpdates",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectWeeklyUpdates_ProjectId_AuthorId_WeekOf",
                table: "ProjectWeeklyUpdates",
                columns: new[] { "ProjectId", "AuthorId", "WeekOf" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectWeeklyUpdates");

            migrationBuilder.DropColumn(
                name: "EstimatedBudget",
                table: "Projects");
        }
    }
}
