using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class UrlRestructure_SessionNoteSlugScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_WorldId_Slug_Root",
                table: "Articles");

            // Backfill GUID-shaped session note slugs to title-derived slugs.
            // Gated on the GUID pattern so re-running is safe: rows with non-GUID slugs are skipped.
            // Within-session slug collisions are resolved by appending -2, -3, … ordered by CreatedAt.
            migrationBuilder.Sql(@"
DECLARE @id UNIQUEIDENTIFIER;
DECLARE @title NVARCHAR(500);
DECLARE @slug NVARCHAR(200);
DECLARE @out NVARCHAR(200);
DECLARE @i INT;
DECLARE @len INT;
DECLARE @c NCHAR(1);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT Id, ISNULL(Title, N'')
    FROM Articles
    WHERE Type = 11
      AND SessionId IS NOT NULL
      AND LEN(Slug) = 36
      AND Slug LIKE N'________-____-____-____-____________';

OPEN cur;
FETCH NEXT FROM cur INTO @id, @title;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @slug = LOWER(REPLACE(@title, N'''', N''));
    SET @out = N'';
    SET @i = 1;
    SET @len = LEN(@slug);

    WHILE @i <= @len
    BEGIN
        SET @c = SUBSTRING(@slug, @i, 1);
        SET @out = @out + CASE WHEN @c LIKE N'[a-z0-9]' THEN @c ELSE N'-' END;
        SET @i = @i + 1;
    END;

    SET @slug = @out;

    WHILE CHARINDEX(N'--', @slug) > 0
        SET @slug = REPLACE(@slug, N'--', N'-');

    WHILE LEN(@slug) > 0 AND LEFT(@slug, 1) = N'-'
        SET @slug = SUBSTRING(@slug, 2, LEN(@slug));

    WHILE LEN(@slug) > 0 AND RIGHT(@slug, 1) = N'-'
        SET @slug = LEFT(@slug, LEN(@slug) - 1);

    IF LEN(@slug) = 0 SET @slug = N'note';
    IF LEN(@slug) > 200 SET @slug = LEFT(@slug, 200);

    UPDATE Articles SET Slug = @slug WHERE Id = @id;

    FETCH NEXT FROM cur INTO @id, @title;
END;

CLOSE cur;
DEALLOCATE cur;

-- Resolve within-session slug collisions (earliest CreatedAt keeps the base slug).
;WITH Ranked AS (
    SELECT
        Id,
        Slug,
        ROW_NUMBER() OVER (PARTITION BY SessionId, Slug ORDER BY CreatedAt) AS rn
    FROM Articles
    WHERE Type = 11 AND SessionId IS NOT NULL
)
UPDATE a
SET a.Slug = r.Slug + N'-' + CAST(r.rn AS NVARCHAR(10))
FROM Articles a
INNER JOIN Ranked r ON a.Id = r.Id
WHERE r.rn > 1;
");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_SessionId_Slug_SessionNote",
                table: "Articles",
                columns: new[] { "SessionId", "Slug" },
                unique: true,
                filter: "[Type] = 11 AND [SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_WorldId_Slug_RootNonSessionNote",
                table: "Articles",
                columns: new[] { "WorldId", "Slug" },
                unique: true,
                filter: "[ParentId] IS NULL AND [Type] <> 11");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // NOTE: slug backfill is not reversed — the original GUID-shaped slugs are not restored.
            migrationBuilder.DropIndex(
                name: "IX_Articles_SessionId_Slug_SessionNote",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_WorldId_Slug_RootNonSessionNote",
                table: "Articles");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_WorldId_Slug_Root",
                table: "Articles",
                columns: new[] { "WorldId", "Slug" },
                unique: true,
                filter: "[ParentId] IS NULL");
        }
    }
}
