using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupKeyToWorldResourceProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LookupKey",
                table: "WorldResourceProviders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorldResourceProviders_WorldId_LookupKey",
                table: "WorldResourceProviders",
                columns: new[] { "WorldId", "LookupKey" },
                unique: true,
                filter: "[LookupKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorldResourceProviders_WorldId_LookupKey",
                table: "WorldResourceProviders");

            migrationBuilder.DropColumn(
                name: "LookupKey",
                table: "WorldResourceProviders");
        }
    }
}
