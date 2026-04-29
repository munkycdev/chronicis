using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class UrlRestructure_SlugFoundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Worlds_OwnerId_Slug",
                table: "Worlds");

            migrationBuilder.DropIndex(
                name: "IX_Worlds_PublicSlug",
                table: "Worlds");

            // Promote PublicSlug → Slug for public worlds before dropping the column.
            migrationBuilder.Sql("""
                UPDATE Worlds
                SET Slug = LOWER(PublicSlug)
                WHERE IsPublic = 1 AND PublicSlug IS NOT NULL AND LEN(PublicSlug) > 0;
                """);

            // Resolve any global slug collisions (e.g. two private worlds with the same
            // per-owner slug now sharing the global namespace) by appending the last 8 hex
            // chars of OwnerId as a disambiguator.
            migrationBuilder.Sql("""
                WITH Ranked AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY Slug ORDER BY CreatedAt) AS rn
                    FROM Worlds
                )
                UPDATE w
                SET w.Slug = w.Slug + '-' + LEFT(REPLACE(CAST(w.OwnerId AS nvarchar(36)), '-', ''), 8)
                FROM Worlds w
                INNER JOIN Ranked r ON w.Id = r.Id
                WHERE r.rn > 1;
                """);

            migrationBuilder.DropColumn(
                name: "PublicSlug",
                table: "Worlds");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Worlds",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "WorldMaps",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Sessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Campaigns",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Arcs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Backfill slugs for Campaign, Arc, Session, WorldMap from their Name columns.
            // Uses a simplified slug (lowercase, spaces → hyphens, apostrophes stripped).
            // A second pass appends -N to resolve any within-scope duplicates.
            migrationBuilder.Sql("""
                UPDATE Campaigns
                SET Slug = LOWER(REPLACE(REPLACE(Name, '''', ''), ' ', '-'));

                WITH Ranked AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY WorldId, Slug ORDER BY CreatedAt) AS rn
                    FROM Campaigns
                )
                UPDATE c
                SET c.Slug = c.Slug + '-' + CAST(r.rn AS nvarchar(10))
                FROM Campaigns c INNER JOIN Ranked r ON c.Id = r.Id
                WHERE r.rn > 1;
                """);

            migrationBuilder.Sql("""
                UPDATE Arcs
                SET Slug = LOWER(REPLACE(REPLACE(Name, '''', ''), ' ', '-'));

                WITH Ranked AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY CampaignId, Slug ORDER BY CreatedAt) AS rn
                    FROM Arcs
                )
                UPDATE a
                SET a.Slug = a.Slug + '-' + CAST(r.rn AS nvarchar(10))
                FROM Arcs a INNER JOIN Ranked r ON a.Id = r.Id
                WHERE r.rn > 1;
                """);

            migrationBuilder.Sql("""
                UPDATE Sessions
                SET Slug = LOWER(REPLACE(REPLACE(Name, '''', ''), ' ', '-'));

                WITH Ranked AS (
                    SELECT Id,
                           ROW_NUMBER() OVER (PARTITION BY ArcId, Slug ORDER BY CreatedAt) AS rn
                    FROM Sessions
                )
                UPDATE s
                SET s.Slug = s.Slug + '-' + CAST(r.rn AS nvarchar(10))
                FROM Sessions s INNER JOIN Ranked r ON s.Id = r.Id
                WHERE r.rn > 1;
                """);

            // WorldMaps uses different column names: WorldMapId (PK) and CreatedUtc (timestamp).
            migrationBuilder.Sql("""
                UPDATE WorldMaps
                SET Slug = LOWER(REPLACE(REPLACE(Name, '''', ''), ' ', '-'));

                WITH Ranked AS (
                    SELECT WorldMapId,
                           ROW_NUMBER() OVER (PARTITION BY WorldId, Slug ORDER BY CreatedUtc) AS rn
                    FROM WorldMaps
                )
                UPDATE wm
                SET wm.Slug = wm.Slug + '-' + CAST(r.rn AS nvarchar(10))
                FROM WorldMaps wm INNER JOIN Ranked r ON wm.WorldMapId = r.WorldMapId
                WHERE r.rn > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Worlds_Slug",
                table: "Worlds",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorldMaps_WorldId_Slug",
                table: "WorldMaps",
                columns: new[] { "WorldId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ArcId_Slug",
                table: "Sessions",
                columns: new[] { "ArcId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_WorldId_Slug",
                table: "Campaigns",
                columns: new[] { "WorldId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Arcs_CampaignId_Slug",
                table: "Arcs",
                columns: new[] { "CampaignId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Worlds_Slug",
                table: "Worlds");

            migrationBuilder.DropIndex(
                name: "IX_WorldMaps_WorldId_Slug",
                table: "WorldMaps");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ArcId_Slug",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_WorldId_Slug",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Arcs_CampaignId_Slug",
                table: "Arcs");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "WorldMaps");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Arcs");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Worlds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "PublicSlug",
                table: "Worlds",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Worlds_OwnerId_Slug",
                table: "Worlds",
                columns: new[] { "OwnerId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Worlds_PublicSlug",
                table: "Worlds",
                column: "PublicSlug",
                unique: true,
                filter: "[PublicSlug] IS NOT NULL");
        }
    }
}
