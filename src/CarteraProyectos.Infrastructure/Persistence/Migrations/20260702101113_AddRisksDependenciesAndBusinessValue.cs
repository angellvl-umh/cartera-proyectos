using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CarteraProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRisksDependenciesAndBusinessValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusinessValue",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    DependsOnProjectId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDependencies_Projects_DependsOnProjectId",
                        column: x => x.DependsOnProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDependencies_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectRisks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Probability = table.Column<string>(type: "text", nullable: false),
                    Impact = table.Column<string>(type: "text", nullable: false),
                    MitigationPlan = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectRisks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectRisks_Persons_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectRisks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependencies_DependsOnProjectId",
                table: "ProjectDependencies",
                column: "DependsOnProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependencies_ProjectId_DependsOnProjectId",
                table: "ProjectDependencies",
                columns: new[] { "ProjectId", "DependsOnProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRisks_CreatedById",
                table: "ProjectRisks",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRisks_ProjectId",
                table: "ProjectRisks",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDependencies");

            migrationBuilder.DropTable(
                name: "ProjectRisks");

            migrationBuilder.DropColumn(
                name: "BusinessValue",
                table: "Projects");
        }
    }
}
