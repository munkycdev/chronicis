using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeQuestSessionCutover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestUpdates_Articles_SessionId",
                table: "QuestUpdates");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestUpdates_Sessions_SessionEntityId",
                table: "QuestUpdates");

            migrationBuilder.DropIndex(
                name: "IX_QuestUpdate_SessionEntityId",
                table: "QuestUpdates");

            migrationBuilder.DropIndex(
                name: "IX_QuestUpdate_SessionId",
                table: "QuestUpdates");

            // Phase 7 cutover: preserve any bridge-only quest associations by copying the canonical
            // SessionEntityId into SessionId before removing the bridge column.
            migrationBuilder.Sql("""
                UPDATE qu
                SET qu.SessionId = qu.SessionEntityId
                FROM QuestUpdates qu
                WHERE qu.SessionEntityId IS NOT NULL
                  AND (qu.SessionId IS NULL OR qu.SessionId <> qu.SessionEntityId);
                """);

            migrationBuilder.DropColumn(
                name: "SessionEntityId",
                table: "QuestUpdates");

            migrationBuilder.CreateIndex(
                name: "IX_QuestUpdate_SessionId",
                table: "QuestUpdates",
                column: "SessionId",
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestUpdates_Sessions_SessionId",
                table: "QuestUpdates",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestUpdates_Sessions_SessionId",
                table: "QuestUpdates");

            migrationBuilder.DropIndex(
                name: "IX_QuestUpdate_SessionId",
                table: "QuestUpdates");

            migrationBuilder.AddColumn<Guid>(
                name: "SessionEntityId",
                table: "QuestUpdates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE qu
                SET qu.SessionEntityId = qu.SessionId
                FROM QuestUpdates qu
                WHERE qu.SessionId IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_QuestUpdate_SessionEntityId",
                table: "QuestUpdates",
                column: "SessionEntityId",
                filter: "[SessionEntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_QuestUpdate_SessionId",
                table: "QuestUpdates",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestUpdates_Articles_SessionId",
                table: "QuestUpdates",
                column: "SessionId",
                principalTable: "Articles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestUpdates_Sessions_SessionEntityId",
                table: "QuestUpdates",
                column: "SessionEntityId",
                principalTable: "Sessions",
                principalColumn: "Id");
        }
    }
}
