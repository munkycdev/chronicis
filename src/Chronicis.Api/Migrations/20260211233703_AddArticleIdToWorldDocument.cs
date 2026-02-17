using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleIdToWorldDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ArticleId",
                table: "WorldDocuments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorldDocuments_ArticleId",
                table: "WorldDocuments",
                column: "ArticleId",
                filter: "[ArticleId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_WorldDocuments_Articles_ArticleId",
                table: "WorldDocuments",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorldDocuments_Articles_ArticleId",
                table: "WorldDocuments");

            migrationBuilder.DropIndex(
                name: "IX_WorldDocuments_ArticleId",
                table: "WorldDocuments");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "WorldDocuments");
        }
    }
}
