using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixRootSlugUniquenessPerWorld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_Slug_Root",
                table: "Articles");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_WorldId_Slug_Root",
                table: "Articles",
                columns: new[] { "WorldId", "Slug" },
                unique: true,
                filter: "[ParentId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_WorldId_Slug_Root",
                table: "Articles");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Slug_Root",
                table: "Articles",
                column: "Slug",
                unique: true,
                filter: "[ParentId] IS NULL");
        }
    }
}
