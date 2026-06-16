using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CarteraProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Projects",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<string>(
                name: "RequestingUnit",
                table: "Projects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "BeneficiaryCount",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DesiredDeploymentDate",
                table: "Projects",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EpicUrl",
                table: "Projects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupPriority",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganicUnitId",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousReferenceId",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromoterId",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiptGroup",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificationsUrl",
                table: "Projects",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UorOrder",
                table: "Projects",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrganicUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganicUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectNotes_Persons_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Persons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectNotes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Promoters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promoters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTags",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    TagsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTags", x => new { x.ProjectId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_ProjectTags_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganicUnitId",
                table: "Projects",
                column: "OrganicUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PromoterId",
                table: "Projects",
                column: "PromoterId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganicUnits_Name",
                table: "OrganicUnits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_AuthorId",
                table: "ProjectNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectNotes_ProjectId",
                table: "ProjectNotes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTags_TagsId",
                table: "ProjectTags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_Promoters_Name",
                table: "Promoters",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            // Migrar valores de Status a los nuevos nombres del enum
            migrationBuilder.Sql(@"
                UPDATE ""Projects""
                SET ""Status"" = 'Stopped'
                WHERE ""Status"" IN ('Proposed', 'Approved', 'InProgress', 'Paused', 'Cancelled');
            ");

            // Migrar valores de Complexity a los nuevos nombres del enum
            migrationBuilder.Sql(@"
                UPDATE ""Projects"" SET ""Complexity"" = 'VerySmall' WHERE ""Complexity"" = 'Low';
                UPDATE ""Projects"" SET ""Complexity"" = 'Large'     WHERE ""Complexity"" = 'High';
                UPDATE ""Projects"" SET ""Complexity"" = 'Large'     WHERE ""Complexity"" = 'VeryHigh';
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_OrganicUnits_OrganicUnitId",
                table: "Projects",
                column: "OrganicUnitId",
                principalTable: "OrganicUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Promoters_PromoterId",
                table: "Projects",
                column: "PromoterId",
                principalTable: "Promoters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_OrganicUnits_OrganicUnitId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Promoters_PromoterId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "OrganicUnits");

            migrationBuilder.DropTable(
                name: "ProjectNotes");

            migrationBuilder.DropTable(
                name: "ProjectTags");

            migrationBuilder.DropTable(
                name: "Promoters");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OrganicUnitId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_PromoterId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "BeneficiaryCount",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DesiredDeploymentDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EpicUrl",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "GroupPriority",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OrganicUnitId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PreviousReferenceId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PromoterId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SiptGroup",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SpecificationsUrl",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UorOrder",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Projects",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "RequestingUnit",
                table: "Projects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
