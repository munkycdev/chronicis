using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHandwrittenNoteImageIdToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HandwrittenNoteImageId",
                table: "Articles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_HandwrittenNoteImageId",
                table: "Articles",
                column: "HandwrittenNoteImageId",
                filter: "[HandwrittenNoteImageId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_WorldDocuments_HandwrittenNoteImageId",
                table: "Articles",
                column: "HandwrittenNoteImageId",
                principalTable: "WorldDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_WorldDocuments_HandwrittenNoteImageId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_HandwrittenNoteImageId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "HandwrittenNoteImageId",
                table: "Articles");
        }
    }
}
