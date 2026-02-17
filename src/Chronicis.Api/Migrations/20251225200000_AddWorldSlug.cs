using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add Slug column as nullable first
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Worlds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // Step 2: Populate existing worlds with slugs derived from names
            // Uses SQL to generate slugs: lowercase, replace spaces with hyphens, remove special chars
            migrationBuilder.Sql(@"
                UPDATE Worlds 
                SET Slug = LOWER(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(Name, ' ', '-'),
                                        '''', ''),
                                    '""', ''),
                                ',', ''),
                            '.', ''),
                        ':', '')
                )
                WHERE Slug IS NULL;
                
                -- Handle any remaining empty slugs
                UPDATE Worlds 
                SET Slug = LOWER(CONVERT(nvarchar(36), Id))
                WHERE Slug IS NULL OR Slug = '';
            ");

            // Step 3: Handle duplicate slugs for the same owner by appending a number
            migrationBuilder.Sql(@"
                WITH DuplicateSlugs AS (
                    SELECT Id, OwnerId, Slug,
                           ROW_NUMBER() OVER (PARTITION BY OwnerId, Slug ORDER BY CreatedAt) as RowNum
                    FROM Worlds
                )
                UPDATE Worlds
                SET Slug = Worlds.Slug + '-' + CAST(ds.RowNum AS nvarchar(10))
                FROM Worlds
                INNER JOIN DuplicateSlugs ds ON Worlds.Id = ds.Id
                WHERE ds.RowNum > 1;
            ");

            // Step 4: Make the column non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Worlds",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Step 5: Add unique index per owner
            migrationBuilder.CreateIndex(
                name: "IX_Worlds_OwnerId_Slug",
                table: "Worlds",
                columns: new[] { "OwnerId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Worlds_OwnerId_Slug",
                table: "Worlds");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Worlds");
        }
    }
}
