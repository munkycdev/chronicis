using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMapFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MapFeatures",
                columns: table => new
                {
                    MapFeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MapLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    LinkedArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapFeatures", x => x.MapFeatureId);
                    table.ForeignKey(
                        name: "FK_MapFeatures_Articles_LinkedArticleId",
                        column: x => x.LinkedArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MapFeatures_MapLayers_MapLayerId",
                        column: x => x.MapLayerId,
                        principalTable: "MapLayers",
                        principalColumn: "MapLayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MapFeatures_WorldMaps_WorldMapId",
                        column: x => x.WorldMapId,
                        principalTable: "WorldMaps",
                        principalColumn: "WorldMapId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MapFeatures_LinkedArticleId",
                table: "MapFeatures",
                column: "LinkedArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_MapFeatures_MapLayerId",
                table: "MapFeatures",
                column: "MapLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MapFeatures_WorldMapId",
                table: "MapFeatures",
                column: "WorldMapId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapFeatures");
        }
    }
}
