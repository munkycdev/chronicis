using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chronicis.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryTemplatesAndConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AISummary",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AISummaryGeneratedAt",
                table: "Campaigns",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryCustomPrompt",
                table: "Campaigns",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SummaryIncludeWebSources",
                table: "Campaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SummaryTemplateId",
                table: "Campaigns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryCustomPrompt",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SummaryIncludeWebSources",
                table: "Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SummaryTemplateId",
                table: "Articles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AISummary",
                table: "Arcs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AISummaryGeneratedAt",
                table: "Arcs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryCustomPrompt",
                table: "Arcs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SummaryIncludeWebSources",
                table: "Arcs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SummaryTemplateId",
                table: "Arcs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SummaryTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorldId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PromptTemplate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SummaryTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SummaryTemplates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SummaryTemplates_Worlds_WorldId",
                        column: x => x.WorldId,
                        principalTable: "Worlds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_SummaryTemplateId",
                table: "Campaigns",
                column: "SummaryTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_SummaryTemplateId",
                table: "Articles",
                column: "SummaryTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Arcs_SummaryTemplateId",
                table: "Arcs",
                column: "SummaryTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SummaryTemplates_CreatedBy",
                table: "SummaryTemplates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SummaryTemplates_IsSystem",
                table: "SummaryTemplates",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_SummaryTemplates_WorldId",
                table: "SummaryTemplates",
                column: "WorldId");

            migrationBuilder.AddForeignKey(
                name: "FK_Arcs_SummaryTemplates_SummaryTemplateId",
                table: "Arcs",
                column: "SummaryTemplateId",
                principalTable: "SummaryTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_SummaryTemplates_SummaryTemplateId",
                table: "Articles",
                column: "SummaryTemplateId",
                principalTable: "SummaryTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Campaigns_SummaryTemplates_SummaryTemplateId",
                table: "Campaigns",
                column: "SummaryTemplateId",
                principalTable: "SummaryTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Seed system templates
            SeedSystemTemplates(migrationBuilder);
        }

        private void SeedSystemTemplates(MigrationBuilder migrationBuilder)
        {
            var defaultTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var characterTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var locationTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var bestiaryTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var factionTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var campaignRecapTemplateId = Guid.Parse("00000000-0000-0000-0000-000000000006");

            // Default template
            migrationBuilder.InsertData(
                table: "SummaryTemplates",
                columns: new[] { "Id", "WorldId", "Name", "Description", "PromptTemplate", "IsSystem", "CreatedBy", "CreatedAt" },
                values: new object[] {
                    defaultTemplateId,
                    null,
                    "Default",
                    "General purpose summary suitable for most content types.",
                    @"You are analyzing tabletop RPG campaign notes to create a comprehensive summary about: {EntityName}

This entity is mentioned in the following campaign notes:

{SourceContent}

{WebContent}

Based on all mentions above, provide a 2-4 paragraph summary including:
1. Who/what this entity is (identity, nature, role)
2. Key relationships with other entities
3. Important events involving this entity
4. Current status or last known information

Focus on facts from the notes. If information conflicts between sources, note the discrepancy.
Keep the tone informative and campaign-focused.",
                    true,
                    null,
                    DateTime.UtcNow
                });

            // Character template
            migrationBuilder.InsertData(
                table: "SummaryTemplates",
                columns: new[] { "Id", "WorldId", "Name", "Description", "PromptTemplate", "IsSystem", "CreatedBy", "CreatedAt" },
                values: new object[] {
                    characterTemplateId,
                    null,
                    "Character",
                    "Focused on personality, motivations, relationships, and character development.",
                    @"You are analyzing tabletop RPG campaign notes to create a character profile for: {EntityName}

This character is mentioned in the following campaign notes:

{SourceContent}

{WebContent}

Create a character summary (2-4 paragraphs) covering:
1. **Identity & Appearance**: Who they are, what they look like, their role in the story
2. **Personality & Motivations**: What drives them, their quirks, how they interact with others
3. **Key Relationships**: Important connections to other characters (allies, enemies, family)
4. **Character Arc**: How they've changed or developed throughout the campaign
5. **Current Status**: Where they are now, what they're doing, any unresolved threads

Write in a way that would help a player or DM quickly understand this character's significance to the campaign.",
                    true,
                    null,
                    DateTime.UtcNow
                });

            // Location template
            migrationBuilder.InsertData(
                table: "SummaryTemplates",
                columns: new[] { "Id", "WorldId", "Name", "Description", "PromptTemplate", "IsSystem", "CreatedBy", "CreatedAt" },
                values: new object[] {
                    locationTemplateId,
                    null,
                    "Location",
                    "Focused on geography, inhabitants, history, and significance to the story.",
                    @"You are analyzing tabletop RPG campaign notes to create a location guide for: {EntityName}

This location is mentioned in the following campaign notes:

{SourceContent}

{WebContent}

Create a location summary (2-4 paragraphs) covering:
1. **Overview**: What kind of place this is, its general atmosphere and feel
2. **Geography & Layout**: Physical description, notable landmarks, surrounding areas
3. **Inhabitants**: Who lives or operates here, important NPCs associated with the location
4. **History & Significance**: Why this place matters to the campaign, key events that happened here
5. **Current State**: What's happening there now, any ongoing situations or dangers

Write in a way that would help a DM set the scene or a player understand why this location matters.",
                    true,
                    null,
                    DateTime.UtcNow
                });

            // Bestiary template
            migrationBuilder.InsertData(
                table: "SummaryTemplates",
                columns: new[] { "Id", "WorldId", "Name", "Description", "PromptTemplate", "IsSystem", "CreatedBy", "CreatedAt" },
                values: new object[] {
                    bestiaryTemplateId,
                    null,
                    "Bestiary",
                    "Focused on creature nature, abilities, behavior, and encounters.",
                    @"You are analyzing tabletop RPG campaign notes to create a bestiary entry for: {EntityName}

This creature is mentioned in the following campaign notes:

{SourceContent}

{WebContent}

Create a bestiary entry (2-4 paragraphs) covering:
1. **Nature & Description**: What kind of creature this is, its appearance and demeanor
2. **Abilities & Tactics**: Known powers, combat behavior, strengths and weaknesses
3. **Habitat & Behavior**: Where they're found, how they act, what they want
4. **Encounters**: Notable encounters the party has had with this creature type
5. **Lore & Secrets**: Any discovered information about their origins, society, or vulnerabilities

Include any campaign-specific variations from standard lore. This should help players prepare for future encounters.",
                    true,
                    null,
                    DateTime.UtcNow
                });

            // Faction template
            migrationBuilder.InsertData(
                table: "SummaryTemplates",
                columns: new[] { "Id", "WorldId", "Name", "Description", "PromptTemplate", "IsSystem", "CreatedBy", "CreatedAt" },
                values: new object[] {
                    factionTemplateId,
                    null,
                    "Faction",
                    "Focused on members, goals, alliances, conflicts, and influence.",
                    @"You are analyzing tabletop RPG campaign notes to create a faction profile for: {EntityName}

This faction is mentioned in the following campaign notes:

{SourceContent}

{WebContent}

Create a faction summary (2-4 paragraphs) covering:
1. **Overview**: What this faction is, their public face and reputation
2. **Leadership & Members**: Key figures, notable members the party has encountered
3. **Goals & Methods**: What they want, how they operate, their resources
4. **Alliances & Enemies**: Relationships with other factions, ongoing conflicts
5. **Party Relations**: How the party has interacted with this faction, current standing

Highlight any secrets the party has discovered about the faction's true nature or hidden agendas.",
                    true,
                    null,
                    DateTime.UtcNow
                });

            // Campaign Recap template
            migrationBuilder.InsertData(
                table: "SummaryTemplates",
                columns: new[] { "Id", "WorldId", "Name", "Description", "PromptTemplate", "IsSystem", "CreatedBy", "CreatedAt" },
                values: new object[] {
                    campaignRecapTemplateId,
                    null,
                    "Campaign Recap",
                    "Summarizes story progression, major events, and plot threads for campaigns and arcs.",
                    @"You are analyzing tabletop RPG session notes to create a campaign/arc recap for: {EntityName}

Here are the session notes to summarize:

{SourceContent}

{WebContent}

Create a narrative recap (3-5 paragraphs) covering:
1. **Story So Far**: The major plot developments and how the story has progressed
2. **Key Events**: The most significant moments, victories, and setbacks
3. **Character Moments**: Important character development, decisions, and roleplay highlights
4. **Active Plot Threads**: Unresolved mysteries, ongoing quests, looming threats
5. **Current Situation**: Where things stand now, what the party is facing next

Write in an engaging narrative style that could be read aloud at the start of a session to remind players where they left off.",
                    true,
                    null,
                    DateTime.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Arcs_SummaryTemplates_SummaryTemplateId",
                table: "Arcs");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_SummaryTemplates_SummaryTemplateId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_SummaryTemplates_SummaryTemplateId",
                table: "Campaigns");

            migrationBuilder.DropTable(
                name: "SummaryTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Campaigns_SummaryTemplateId",
                table: "Campaigns");

            migrationBuilder.DropIndex(
                name: "IX_Articles_SummaryTemplateId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Arcs_SummaryTemplateId",
                table: "Arcs");

            migrationBuilder.DropColumn(
                name: "AISummary",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "AISummaryGeneratedAt",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "SummaryCustomPrompt",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "SummaryIncludeWebSources",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "SummaryTemplateId",
                table: "Campaigns");

            migrationBuilder.DropColumn(
                name: "SummaryCustomPrompt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SummaryIncludeWebSources",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SummaryTemplateId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "AISummary",
                table: "Arcs");

            migrationBuilder.DropColumn(
                name: "AISummaryGeneratedAt",
                table: "Arcs");

            migrationBuilder.DropColumn(
                name: "SummaryCustomPrompt",
                table: "Arcs");

            migrationBuilder.DropColumn(
                name: "SummaryIncludeWebSources",
                table: "Arcs");

            migrationBuilder.DropColumn(
                name: "SummaryTemplateId",
                table: "Arcs");
        }
    }
}
