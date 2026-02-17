using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleLinks_Articles_SourceArticleId",
                        column: x => x.SourceArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleLinks_Articles_TargetArticleId",
                        column: x => x.TargetArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticleLinks_SourceArticleId",
                table: "ArticleLinks",
                column: "SourceArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleLinks_SourceArticleId_TargetArticleId_Position",
                table: "ArticleLinks",
                columns: new[] { "SourceArticleId", "TargetArticleId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleLinks_TargetArticleId",
                table: "ArticleLinks",
                column: "TargetArticleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleLinks");
        }
    }
}
