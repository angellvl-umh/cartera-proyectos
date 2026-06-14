using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CarteraProyectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkItemEmbeddings",
                columns: table => new
                {
                    WorkItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: false),
                    TextSnapshot = table.Column<string>(type: "character varying(8200)", maxLength: 8200, nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemEmbeddings", x => x.WorkItemId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkItemEmbeddings");
        }
    }
}
