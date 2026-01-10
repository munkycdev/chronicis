# Chronicis Quick Start Guide

Welcome to Chronicis! This guide will help you get started managing your tabletop RPG campaigns.

## First Time Setup

### 1. Create Your First World

When you first log in, you'll see the Dashboard with two options:

1. **Create New World** - Start a new campaign world
2. **Join a World** - Use an invitation code to join someone else's world

Click **"Create New World"** and give your world a name (e.g., "Forgotten Realms Campaign").

### 2. Create a Campaign

Once you have a world:

1. Navigate to your world's detail page
2. Click **"New Campaign"** in the Quick Actions section
3. Enter a campaign name (e.g., "Dragon Heist")
4. Your campaign appears in the tree navigation

### 3. Add Your First Article

There are several ways to create content:

**From the World Detail Page:**
- Click **"New Wiki Article"** for world lore
- Click **"New Player Character"** for character profiles

**From the Tree Navigation:**
- Right-click any folder
- Select "Add Child"
- Choose the article type

**Tip:** Articles are automatically organized by type:
- Characters → Player Characters section
- Wiki articles → Wiki section
- Sessions → Within campaigns and arcs

## Core Features

### Writing Content

Chronicis uses a rich text editor with markdown support:

**Formatting:**
- **Bold:** `**text**` or Ctrl+B
- *Italic:* `*text*` or Ctrl+I
- Headings: `# Heading` or `## Subheading`
- Lists: Start line with `-` or `1.`
- Code blocks: Triple backticks

**Auto-save:** Your content saves automatically as you type (500ms delay).

### Linking Articles

Create connections between articles using wiki-style links:

1. Type `[[` to open autocomplete
2. Start typing the article name
3. Select from the list or create a new article
4. Links appear as clickable references

**Example:** `[[Waterdeep]]` creates a link to your Waterdeep article.

**Backlinks:** The right sidebar shows all articles that mention the current article.

### Linking External Resources

You can link to external reference content (like the D&D 5e SRD) using the same wiki-link flow.

1. Type `[[srd/` to switch autocomplete to SRD mode
2. Keep typing to filter results (spells, monsters, etc.)
3. Press Enter to insert the selected entry

External links render as a chip with an "external" indicator so you can tell they are not stored in your world.

**Example:** `[[srd/acid arrow]]` inserts a chip for *Acid Arrow*.

**Previewing External Content:**
- Click the external chip to open an in-app preview drawer
- The preview shows the SRD content returned by the source API
- Use "Open on source site" to view it in a new tab

### Uploading Documents

Share maps, character sheets, handouts, and other files:

1. Navigate to your world's detail page
2. Scroll to the **Documents** section
3. Click **"Upload Document"**
4. Select your file (PDF, Word, Excel, PowerPoint, images, text)
5. Add an optional description
6. Click **"Upload"**

**Supported Files:**
- Documents: PDF, Word (.docx), Text, Markdown
- Spreadsheets: Excel (.xlsx)
- Presentations: PowerPoint (.pptx)
- Images: PNG, JPEG, GIF, WebP

**Max Size:** 200 MB per file

**Accessing Documents:**
- Click the document in the tree navigation to download
- Use the download button in the Documents section
- Documents are available to all world members

**Managing Documents (GM Only):**
- Edit titles and descriptions by clicking the edit icon
- Delete documents you no longer need
- Documents appear in the "External Resources" section of the tree

### Organizing Content

**Hierarchical Structure:**
- Worlds contain Campaigns
- Campaigns contain Arcs
- Arcs contain Sessions
- Everything can have notes and wiki articles

**Drag & Drop:**
- Reorganize articles by dragging them in the tree
- Move articles between sections
- Create nested hierarchies

### Privacy Controls

Control who can see your content:

**Article Privacy:**
1. Click the article you want to make private
2. Open the metadata panel (right sidebar)
3. Toggle "Private"
4. Private articles show a 🔒 lock icon

**Note:** Only you can see private articles. Other world members can't access them.

### Search

Find content quickly:

**Title Search (Left Sidebar):**
- Type in the search box at the top of the tree
- Articles matching your search appear
- Ancestors auto-expand to show context

**Full-Text Search (Top Bar):**
- Use the search box in the app header
- Searches titles, content, and links
- Shows context snippets with matches highlighted

## Collaboration

### Inviting Players

Share your world with others:

1. Navigate to your world's detail page
2. Scroll to **"Members & Invitations"**
3. Click **"Create Invitation"**
4. Copy the invitation code (format: XXXX-XXXX)
5. Share the code with your players

**Players Join:**
1. From the Dashboard, click **"Join a World"**
2. Enter the invitation code
3. Click "Join"

**Managing Members:**
- View all members in the Members section
- Change player roles (GM, Player, Observer)
- Remove members if needed
- Revoke unused invitations

### Roles & Permissions

**Game Master (GM):**
- Full access to all content
- Can upload/delete documents
- Can manage members and invitations
- Can make the world public
- Can export world data

**Player:**
- Can view all public and members-only articles
- Can create and edit their own articles
- Can view and download documents
- Can export world data
- Cannot upload or delete documents

**Observer:**
- Read-only access
- Can view but not edit content
- Can export world data

## Public Sharing

Share your world with the broader community:

### Making Your World Public

1. Navigate to your world's detail page
2. Scroll to **"Public Sharing"**
3. Toggle **"Make this world publicly accessible"**
4. Enter a unique URL slug (e.g., "my-dragon-heist-campaign")
5. The system checks if your slug is available
6. Copy the public URL to share

**Article Visibility:**
- **Public:** Anyone with the link can view
- **Members Only:** Only world members can view
- **Private:** Only you can view

Set visibility in the article's metadata panel (right sidebar).

### Previewing Public View

Click the **"Preview"** button in the Public Sharing section to see what others will see.

## AI Features

### Generate Article Summaries

Get AI-powered summaries of articles and their connections:

1. Open an article
2. Expand **"AI Summary"** in the right sidebar
3. Review cost estimate (credits required)
4. Click **"Generate Summary"**
5. AI analyzes the article and all articles that mention it

**What It Does:**
- Summarizes the article's content
- Includes context from linked articles
- Shows relationships and mentions
- Can be regenerated or copied to clipboard

**Note:** AI summaries use Azure OpenAI and consume credits. Check the estimate before generating.

## Exporting Your Data

### Export World to Markdown

Backup your world or migrate to other tools like Obsidian:

1. Click your profile icon in the top right
2. Select **"Settings"**
3. Go to the **"Data"** tab
4. Select the world you want to export from the dropdown
5. Click **"Export to Markdown"**
6. Wait for the export to complete (may take a moment for large worlds)
7. Your browser will automatically download a zip file

**What's Included:**
- All articles organized in folders matching your tree structure
- YAML frontmatter with metadata (title, type, dates, visibility)
- AI summaries included at the bottom of each article
- Campaigns and Arcs in their own folders
- Wiki links converted to `[[Article Name]]` format

**Folder Structure:**
```
YourWorld/
├── Wiki/
│   └── Locations/
│       └── Waterdeep/
│           └── Waterdeep.md
├── Characters/
│   └── Hero Name/
│       └── Hero Name.md
└── Campaigns/
    └── Dragon Heist/
        ├── Dragon Heist.md
        └── Chapter 1/
            ├── Chapter 1.md
            └── Session 1/
                └── Session 1.md
```

**Use Cases:**
- **Backup:** Keep a local copy of your campaign data
- **Migration:** Move to Obsidian, Notion, or other markdown tools
- **Sharing:** Send campaign files to players who prefer offline access
- **Archival:** Preserve completed campaigns

## Settings

Access settings via the profile menu in the top right corner.

### Profile Tab
View your account information including:
- Display name and email from your login provider
- Avatar (from Google, Discord, etc.)

### Data Tab
- **Export World Data:** Download your world as markdown files (see above)
- **Import Data:** Coming soon - import from Obsidian, Notion, and other tools

### Preferences Tab
Customization options (coming soon):
- Theme settings and dark mode
- Editor preferences
- Notification settings

## Tips & Best Practices

1. **Use Descriptive Names:** Clear article titles make navigation easier
2. **Link Liberally:** Connect related articles with `[[wiki links]]`
3. **Add Descriptions:** Help players understand documents and articles
4. **Tag Important NPCs:** Create character articles for recurring NPCs
5. **Session Notes:** Record session summaries to build campaign history
6. **Mix Internal and External Links:** Use internal articles for campaign-specific lore, and external links for reference rules and stat blocks.


### Document Management

1. **Name Documents Clearly:** "Session 5 Battle Map" vs "map.pdf"
2. **Add Descriptions:** Explain what the document contains
3. **Organize by Prefix:** Use "Map - ", "NPC - ", "Handout - " prefixes
4. **Check File Sizes:** Compress large PDFs before uploading
5. **Delete Outdated Files:** Remove documents you no longer need

### Collaboration

1. **Set Expectations:** Explain privacy settings to your players
2. **Use Roles Wisely:** Give trusted players GM access if they co-DM
3. **Regular Backlinks Check:** See which articles mention important NPCs/locations
4. **Session Prep:** Upload maps and handouts before sessions
5. **Encourage Player Notes:** Let players create their own character notes

### Privacy Strategy

1. **GM Secrets:** Mark plot twists and hidden info as Private
2. **Player Visible:** Keep NPC stats and locations Members Only
3. **Public Sharing:** Only make spoiler-free content Public
4. **Review Before Publishing:** Check all articles before making world public

## Common Workflows

### Session Prep Workflow

1. Create new Session article in current Arc
2. Upload battle maps to Documents
3. Create NPC articles for new characters
4. Link NPCs and locations in session notes
5. Mark GM-only notes as Private

### Post-Session Workflow

1. Update session notes with what happened
2. Create articles for new discoveries
3. Update character notes with level-ups
4. Upload any new handouts or maps
5. Generate AI summary of major events

### Character Creation Workflow

1. Click "New Player Character" from world detail
2. Fill in character details
3. Upload character sheet PDF to Documents
4. Link to relevant location/faction articles
5. Share character with other players (Members Only visibility)

### World Building Workflow

1. Create location hierarchy (Continent → Region → City → District)
2. Create faction/organization articles
3. Link factions to locations
4. Add NPC articles with faction affiliations
5. Use backlinks to see faction membership

## Troubleshooting

**Can't see articles in tree:**
- Check if you're a member of the world
- Ask GM to verify your membership
- Try refreshing the page

**Upload fails:**
- Check file size (max 200 MB)
- Verify file type is supported
- Ensure you're the world owner (GM)

**Links not working:**
- Make sure article names match exactly
- Check for typos in `[[link]]` syntax
- Verify target article exists

**Search not finding articles:**
- Check article visibility settings
- Try searching with different terms
- Use title search vs. full-text search

**Players can't join:**
- Verify invitation code hasn't been revoked
- Check code wasn't mistyped (case-sensitive)
- Ensure invitation hasn't expired

## Next Steps

Now that you know the basics:

1. **Explore the Dashboard:** See your world statistics and recent activity
2. **Try AI Summaries:** Generate summaries for key articles
3. **Set Up Public Sharing:** Share your world with the community
4. **Invite Your Players:** Get your gaming group on Chronicis
5. **Upload Session Materials:** Add maps, handouts, and character sheets

## Need Help?

- Check the [User Documentation](Document-Storage-User-Guide.md) for detailed guides
- Review the [Changelog](CHANGELOG.md) for latest features
- Join the Chronicis community for tips and support
- Submit feedback via the app feedback button

**Happy chronicling!** 🐉📚✨
