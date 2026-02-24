using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionEntity_Snapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SessionEntityId",
                table: "QuestUpdates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "Articles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArcId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublicNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrivateNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiSummaryGeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AiSummaryGeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Arcs_ArcId",
                        column: x => x.ArcId,
                        principalTable: "Arcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_AiSummaryGeneratedByUserId",
                        column: x => x.AiSummaryGeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestUpdate_SessionEntityId",
                table: "QuestUpdates",
                column: "SessionEntityId",
                filter: "[SessionEntityId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_SessionId",
                table: "Articles",
                column: "SessionId",
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AiSummaryGeneratedByUserId",
                table: "Sessions",
                column: "AiSummaryGeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ArcId",
                table: "Sessions",
                column: "ArcId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ArcId_SessionDate",
                table: "Sessions",
                columns: new[] { "ArcId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CreatedBy",
                table: "Sessions",
                column: "CreatedBy");

            // ─────────────────────────────────────────────────────────────────
            // Backfill: migrate legacy Session articles → Session entities
            //
            // For each Article where Type = 10 (ArticleType.Session):
            //   - Insert a Session row copying Id, ArcId, Title (→ Name),
            //     SessionDate, CreatedAt, CreatedBy, and treating Body as PublicNotes.
            //   - Articles with NULL ArcId are skipped (orphaned; safe on empty DB).
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                INSERT INTO Sessions (Id, ArcId, Name, SessionDate, PublicNotes, PrivateNotes,
                                      AiSummary, AiSummaryGeneratedAt, AiSummaryGeneratedByUserId,
                                      CreatedAt, ModifiedAt, CreatedBy)
                SELECT
                    a.Id,
                    a.ArcId,
                    a.Title,
                    a.SessionDate,
                    a.Body,   -- legacy body becomes PublicNotes (DM canonical content)
                    NULL,     -- PrivateNotes: none on legacy articles
                    a.AISummary,
                    a.AISummaryGeneratedAt,
                    NULL,     -- AiSummaryGeneratedByUserId: unknown for legacy rows
                    a.CreatedAt,
                    a.ModifiedAt,
                    a.CreatedBy
                FROM Articles a
                WHERE a.Type = 10   -- ArticleType.Session
                  AND a.ArcId IS NOT NULL;
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Sessions_SessionId",
                table: "Articles",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_QuestUpdates_Sessions_SessionEntityId",
                table: "QuestUpdates",
                column: "SessionEntityId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            // ─────────────────────────────────────────────────────────────────
            // Backfill Articles.SessionId for SessionNote articles
            //
            // A SessionNote (Type = 11) whose ParentId was a legacy Session article
            // (Type = 10) should be linked to the Session entity created above.
            // The Session.Id == the legacy Article.Id (Ids were preserved in insert).
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                UPDATE notes
                SET notes.SessionId = notes.ParentId
                FROM Articles notes
                INNER JOIN Articles parent ON parent.Id = notes.ParentId
                WHERE notes.Type = 11          -- ArticleType.SessionNote
                  AND parent.Type = 10         -- parent is legacy Session article
                  AND parent.ArcId IS NOT NULL; -- only migrated parents
            ");

            // ─────────────────────────────────────────────────────────────────
            // Backfill QuestUpdates.SessionEntityId
            //
            // QuestUpdate.SessionId currently points to a legacy Session Article.
            // That Article.Id is now also a Session entity Id (preserved above).
            // ─────────────────────────────────────────────────────────────────
            migrationBuilder.Sql(@"
                UPDATE qu
                SET qu.SessionEntityId = qu.SessionId
                FROM QuestUpdates qu
                INNER JOIN Sessions s ON s.Id = qu.SessionId
                WHERE qu.SessionId IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Sessions_SessionId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestUpdates_Sessions_SessionEntityId",
                table: "QuestUpdates");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_QuestUpdate_SessionEntityId",
                table: "QuestUpdates");

            migrationBuilder.DropIndex(
                name: "IX_Articles_SessionId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SessionEntityId",
                table: "QuestUpdates");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "Articles");
        }
    }
}
