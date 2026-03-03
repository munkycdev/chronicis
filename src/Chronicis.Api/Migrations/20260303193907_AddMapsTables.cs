using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMapsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorldMaps",
                columns: table => new
                {
                    WorldMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BasemapBlobKey = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    BasemapContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BasemapOriginalFilename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldMaps", x => x.WorldMapId);
                    table.ForeignKey(
                        name: "FK_WorldMaps_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MapLayers",
                columns: table => new
                {
                    MapLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentLayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MapLayers", x => x.MapLayerId);
                    table.ForeignKey(
                        name: "FK_MapLayers_MapLayers_ParentLayerId",
                        column: x => x.ParentLayerId,
                        principalTable: "MapLayers",
                        principalColumn: "MapLayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MapLayers_WorldMaps_WorldMapId",
                        column: x => x.WorldMapId,
                        principalTable: "WorldMaps",
                        principalColumn: "WorldMapId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorldMapArcs",
                columns: table => new
                {
                    WorldMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArcId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldMapArcs", x => new { x.WorldMapId, x.ArcId });
                    table.ForeignKey(
                        name: "FK_WorldMapArcs_Arcs_ArcId",
                        column: x => x.ArcId,
                        principalTable: "Arcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorldMapArcs_WorldMaps_WorldMapId",
                        column: x => x.WorldMapId,
                        principalTable: "WorldMaps",
                        principalColumn: "WorldMapId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorldMapCampaigns",
                columns: table => new
                {
                    WorldMapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldMapCampaigns", x => new { x.WorldMapId, x.CampaignId });
                    table.ForeignKey(
                        name: "FK_WorldMapCampaigns_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorldMapCampaigns_WorldMaps_WorldMapId",
                        column: x => x.WorldMapId,
                        principalTable: "WorldMaps",
                        principalColumn: "WorldMapId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MapLayers_ParentLayerId",
                table: "MapLayers",
                column: "ParentLayerId");

            migrationBuilder.CreateIndex(
                name: "IX_MapLayers_WorldMapId",
                table: "MapLayers",
                column: "WorldMapId");

            migrationBuilder.CreateIndex(
                name: "IX_WorldMapArcs_ArcId",
                table: "WorldMapArcs",
                column: "ArcId");

            migrationBuilder.CreateIndex(
                name: "IX_WorldMapCampaigns_CampaignId",
                table: "WorldMapCampaigns",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_WorldMaps_WorldId",
                table: "WorldMaps",
                column: "WorldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MapLayers");

            migrationBuilder.DropTable(
                name: "WorldMapArcs");

            migrationBuilder.DropTable(
                name: "WorldMapCampaigns");

            migrationBuilder.DropTable(
                name: "WorldMaps");
        }
    }
}
