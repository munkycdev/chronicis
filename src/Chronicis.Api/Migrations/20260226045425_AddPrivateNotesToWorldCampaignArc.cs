using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateNotesToWorldCampaignArc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrivateNotes",
                table: "Worlds",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivateNotes",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivateNotes",
                table: "Arcs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrivateNotes",
                table: "Worlds");

            migrationBuilder.DropColumn(
                name: "PrivateNotes",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "PrivateNotes",
                table: "Arcs");
        }
    }
}
