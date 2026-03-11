using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolygonMapFeatureSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeatureType",
                table: "MapFeatures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GeometryBlobKey",
                table: "MapFeatures",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeometryETag",
                table: "MapFeatures",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatureType",
                table: "MapFeatures");

            migrationBuilder.DropColumn(
                name: "GeometryBlobKey",
                table: "MapFeatures");

            migrationBuilder.DropColumn(
                name: "GeometryETag",
                table: "MapFeatures");
        }
    }
}
