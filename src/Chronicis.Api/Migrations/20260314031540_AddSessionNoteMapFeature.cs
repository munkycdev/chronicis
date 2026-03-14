using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionNoteMapFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SessionNoteMapFeatures",
                columns: table => new
                {
                    SessionNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MapFeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionNoteMapFeatures", x => new { x.SessionNoteId, x.MapFeatureId });
                    table.ForeignKey(
                        name: "FK_SessionNoteMapFeatures_Articles_SessionNoteId",
                        column: x => x.SessionNoteId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionNoteMapFeatures_MapFeatures_MapFeatureId",
                        column: x => x.MapFeatureId,
                        principalTable: "MapFeatures",
                        principalColumn: "MapFeatureId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SessionNoteMapFeatures_MapFeatureId",
                table: "SessionNoteMapFeatures",
                column: "MapFeatureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SessionNoteMapFeatures");
        }
    }
}
