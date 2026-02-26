using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorialPages : Migration
    {
        private static readonly Guid SystemTutorialSeedUserId = new("7C8A7B43-53B1-4DBA-9EF8-0E85F2C66A2E");
        private static readonly Guid DefaultTutorialArticleId = new("EAF6D621-2DB5-4FA9-9AF0-4D309E3E3B54");
        private static readonly Guid DashboardTutorialArticleId = new("3C5085CC-56D7-4A64-A81B-0C30E4E8B23B");
        private static readonly Guid SettingsTutorialArticleId = new("9F4FD0EC-7D51-46E6-9F0A-6D0133CE2F7A");
        private static readonly Guid AnyArticleTutorialArticleId = new("D514038A-EDAA-486E-BF96-D2E4E5272D46");
        private static readonly Guid DefaultTutorialPageId = new("1F9112C4-3E99-4A4E-BD0E-D59ECA6D49F5");
        private static readonly Guid DashboardTutorialPageId = new("B14830FD-B444-441A-B5DD-552F22A3E160");
        private static readonly Guid SettingsTutorialPageId = new("D0AA40A5-4779-48E1-B43D-8D529C47609B");
        private static readonly Guid AnyArticleTutorialPageId = new("B37F5993-2B28-4CB8-9BA4-52E935B42BD4");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var seededAt = new DateTime(2026, 2, 25, 22, 7, 21, DateTimeKind.Utc);

            migrationBuilder.CreateTable(
                name: "TutorialPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PageTypeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorialPages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorialPages_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TutorialPages_ArticleId",
                table: "TutorialPages",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorialPages_PageType",
                table: "TutorialPages",
                column: "PageType",
                unique: true);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[]
                {
                    "Id",
                    "Auth0UserId",
                    "Email",
                    "DisplayName",
                    "AvatarUrl",
                    "CreatedAt",
                    "LastLoginAt",
                    "HasCompletedOnboarding"
                },
                values: new object[]
                {
                    SystemTutorialSeedUserId,
                    "system|tutorial-seed",
                    "system+tutorial@chronicis.local",
                    "Chronicis System",
                    null,
                    seededAt,
                    seededAt,
                    true
                });

            migrationBuilder.InsertData(
                table: "Worlds",
                columns: new[]
                {
                    "Id",
                    "Name",
                    "Slug",
                    "Description",
                    "OwnerId",
                    "CreatedAt",
                    "IsTutorial",
                    "IsPublic",
                    "PublicSlug"
                },
                values: new object[]
                {
                    Guid.Empty,
                    "System Tutorial World",
                    "system-tutorial",
                    "Sentinel world row for global tutorial articles.",
                    SystemTutorialSeedUserId,
                    seededAt,
                    false,
                    false,
                    null
                });

            migrationBuilder.InsertData(
                table: "Articles",
                columns: new[]
                {
                    "Id",
                    "Title",
                    "Slug",
                    "Body",
                    "Type",
                    "Visibility",
                    "CreatedBy",
                    "CreatedAt",
                    "EffectiveDate",
                    "WorldId",
                    "SummaryIncludeWebSources"
                },
                values: new object[]
                {
                    DefaultTutorialArticleId,
                    "Default Tutorial",
                    "tutorial-default",
                    "<p>Welcome to Chronicis tutorials. This default tutorial is a placeholder and should be replaced by authored content.</p>",
                    100, // ArticleType.Tutorial
                    0,   // ArticleVisibility.Public
                    SystemTutorialSeedUserId,
                    seededAt,
                    seededAt,
                    Guid.Empty,
                    false
                });

            migrationBuilder.InsertData(
                table: "Articles",
                columns: new[]
                {
                    "Id",
                    "Title",
                    "Slug",
                    "Body",
                    "Type",
                    "Visibility",
                    "CreatedBy",
                    "CreatedAt",
                    "EffectiveDate",
                    "WorldId",
                    "SummaryIncludeWebSources"
                },
                values: new object[]
                {
                    DashboardTutorialArticleId,
                    "Dashboard Tutorial",
                    "tutorial-dashboard",
                    "<p>This is a starter tutorial for the dashboard. Replace this placeholder content with authored guidance.</p>",
                    100, // ArticleType.Tutorial
                    0,   // ArticleVisibility.Public
                    SystemTutorialSeedUserId,
                    seededAt,
                    seededAt,
                    Guid.Empty,
                    false
                });

            migrationBuilder.InsertData(
                table: "Articles",
                columns: new[]
                {
                    "Id",
                    "Title",
                    "Slug",
                    "Body",
                    "Type",
                    "Visibility",
                    "CreatedBy",
                    "CreatedAt",
                    "EffectiveDate",
                    "WorldId",
                    "SummaryIncludeWebSources"
                },
                values: new object[]
                {
                    SettingsTutorialArticleId,
                    "Settings Tutorial",
                    "tutorial-settings",
                    "<p>This is a starter tutorial for settings. Replace this placeholder content with authored guidance.</p>",
                    100, // ArticleType.Tutorial
                    0,   // ArticleVisibility.Public
                    SystemTutorialSeedUserId,
                    seededAt,
                    seededAt,
                    Guid.Empty,
                    false
                });

            migrationBuilder.InsertData(
                table: "Articles",
                columns: new[]
                {
                    "Id",
                    "Title",
                    "Slug",
                    "Body",
                    "Type",
                    "Visibility",
                    "CreatedBy",
                    "CreatedAt",
                    "EffectiveDate",
                    "WorldId",
                    "SummaryIncludeWebSources"
                },
                values: new object[]
                {
                    AnyArticleTutorialArticleId,
                    "Any Article Tutorial",
                    "tutorial-article-any",
                    "<p>This is a starter tutorial for article pages. Replace this placeholder content with authored guidance.</p>",
                    100, // ArticleType.Tutorial
                    0,   // ArticleVisibility.Public
                    SystemTutorialSeedUserId,
                    seededAt,
                    seededAt,
                    Guid.Empty,
                    false
                });

            migrationBuilder.InsertData(
                table: "TutorialPages",
                columns: new[]
                {
                    "Id",
                    "PageType",
                    "PageTypeName",
                    "ArticleId",
                    "CreatedAt",
                    "ModifiedAt"
                },
                values: new object[]
                {
                    DefaultTutorialPageId,
                    "Page:Default",
                    "Default Tutorial",
                    DefaultTutorialArticleId,
                    seededAt,
                    seededAt
                });

            migrationBuilder.InsertData(
                table: "TutorialPages",
                columns: new[]
                {
                    "Id",
                    "PageType",
                    "PageTypeName",
                    "ArticleId",
                    "CreatedAt",
                    "ModifiedAt"
                },
                values: new object[]
                {
                    DashboardTutorialPageId,
                    "Page:Dashboard",
                    "Dashboard",
                    DashboardTutorialArticleId,
                    seededAt,
                    seededAt
                });

            migrationBuilder.InsertData(
                table: "TutorialPages",
                columns: new[]
                {
                    "Id",
                    "PageType",
                    "PageTypeName",
                    "ArticleId",
                    "CreatedAt",
                    "ModifiedAt"
                },
                values: new object[]
                {
                    SettingsTutorialPageId,
                    "Page:Settings",
                    "Settings",
                    SettingsTutorialArticleId,
                    seededAt,
                    seededAt
                });

            migrationBuilder.InsertData(
                table: "TutorialPages",
                columns: new[]
                {
                    "Id",
                    "PageType",
                    "PageTypeName",
                    "ArticleId",
                    "CreatedAt",
                    "ModifiedAt"
                },
                values: new object[]
                {
                    AnyArticleTutorialPageId,
                    "ArticleType:Any",
                    "Any Article",
                    AnyArticleTutorialArticleId,
                    seededAt,
                    seededAt
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TutorialPages");

            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: AnyArticleTutorialArticleId);

            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: SettingsTutorialArticleId);

            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: DashboardTutorialArticleId);

            migrationBuilder.DeleteData(
                table: "Articles",
                keyColumn: "Id",
                keyValue: DefaultTutorialArticleId);

            migrationBuilder.DeleteData(
                table: "Worlds",
                keyColumn: "Id",
                keyValue: Guid.Empty);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: SystemTutorialSeedUserId);
        }
    }
}
