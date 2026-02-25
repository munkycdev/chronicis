using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldIsTutorialFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTutorial",
                table: "Worlds",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTutorial",
                table: "Worlds");
        }
    }
}
