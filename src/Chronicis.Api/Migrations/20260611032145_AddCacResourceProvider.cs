using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCacResourceProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ResourceProviders",
                columns: new[] { "Code", "CreatedAt", "Description", "DocumentationLink", "IsActive", "License", "Name" },
                values: new object[] { "cac", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Castles & Crusades source material", "https://trolllord.com/cnc/", true, "https://opengamingfoundation.org/ogl.html", "Castles & Crusades" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ResourceProviders",
                keyColumn: "Code",
                keyValue: "cac");
        }
    }
}
