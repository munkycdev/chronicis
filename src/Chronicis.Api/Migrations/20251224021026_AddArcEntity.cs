using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArcEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ArcId",
                table: "Articles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Arcs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Arcs_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Arcs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ArcId",
                table: "Articles",
                column: "ArcId");

            migrationBuilder.CreateIndex(
                name: "IX_Arcs_CampaignId",
                table: "Arcs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Arcs_CampaignId_SortOrder",
                table: "Arcs",
                columns: new[] { "CampaignId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Arcs_CreatedBy",
                table: "Arcs",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Arcs_ArcId",
                table: "Articles",
                column: "ArcId",
                principalTable: "Arcs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Arcs_ArcId",
                table: "Articles");

            migrationBuilder.DropTable(
                name: "Arcs");

            migrationBuilder.DropIndex(
                name: "IX_Articles_ArcId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ArcId",
                table: "Articles");
        }
    }
}
