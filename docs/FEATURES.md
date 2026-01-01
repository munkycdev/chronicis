# Chronicis - Feature Documentation

**Last Updated:** January 1, 2026

---

## Core Features

### Hierarchical Article Organization

Articles can be nested infinitely deep to mirror campaign structure. Each article belongs to a World and can have a parent article and any number of children.

**Article Types:**
- **WikiArticle** - General content (locations, NPCs, items, lore)
- **Character** - Player or NPC character (top-level in Characters group)
- **CharacterNote** - Notes nested under a Character
- **Session** - Game session notes (must belong to an Arc)
- **SessionNote** - Additional notes under a Session
- **Legacy** - Unmigrated content from old schema

**Virtual Groups in Tree:**
- **Campaigns** - Campaign entities with their Arcs
- **Player Characters** - Top-level Character articles
- **Wiki** - Top-level WikiArticle articles
- **Uncategorized** - Legacy and untyped articles

---

### Wiki-Style Links

Link articles using `[[Article Name]]` syntax, similar to Obsidian.

**Syntax:**
- `[[Article Path]]` - Displays article title, links to article
- `[[Article Path|Display Text]]` - Custom display text

**Autocomplete:**
- Triggered after typing `[[` and 3+ characters
- Scoped to current World only
- Shows up to 10 suggestions, sorted alphabetically
- "Create new article" option when no match found

**Storage:**
- Links stored as `[[guid|Display Text]]` or `[[guid]]` in markdown
- Survives renames since GUID-based
- ArticleLink table tracks all links for backlink queries

**Broken Links:**
- Visual indicator when target article deleted
- Click shows recovery options: find new target, remove link, convert to plain text

---

### Backlinks Panel

The right sidebar shows all articles that link to the current article.

**Display:**
- Article title and path
- Context snippet showing where the link appears
- Click to navigate to source article

**Data:** Populated from ArticleLink table via `/api/articles/{id}/backlinks`

---

### AI Summary Generation

Generate comprehensive summaries of entities by analyzing all backlinks.

**How It Works:**
1. User clicks "Generate Summary" button
2. System collects all articles that link to current article
3. Azure OpenAI analyzes the backlink content
4. Summary generated based on how the entity is mentioned across the campaign

**Configuration:**
- Model: GPT-4.1-mini via Azure OpenAI
- Cost estimation shown before generation
- Custom prompt overrides available

**UI:**
- Collapsible summary section
- Copy, regenerate, and clear actions
- Generation progress indicator

---

### Full-Text Search

Global search across all article content.

**Search Scope:**
- Article titles
- Article body content
- Wiki links

**Results:**
- Grouped by match type (Title, Body, Link matches)
- Context snippets with search term
- Query term highlighting
- Click to navigate to article

**API:** `GET /api/articles/search?query={term}`

---

### Entity Management

#### Worlds
Top-level container for all campaign content. Each user can have multiple Worlds for different settings.

**Fields:** Name, Slug, Description, OwnerId
**Features:** External links (Roll20, D&D Beyond, etc.)

#### Campaigns
Gaming group's collaborative space within a World.

**Fields:** Name, Description, StartedAt, EndedAt, IsActive
**Features:** AI summary, member management (future)

#### Arcs
Story arcs within a Campaign for organizing Sessions.

**Fields:** Name, Description, SortOrder, IsActive
**Features:** Session management

---

### Inline Editing

All content editing follows an Obsidian-like paradigm.

**Behavior:**
- Fields are always editable (no edit mode toggle)
- Body auto-saves 0.5 seconds after last keystroke
- Title saves on blur or Enter key
- No confirmation dialogs for saves

**TipTap Editor:**
- WYSIWYG markdown editing
- Real-time rendering
- Custom styling for headers, lists, code blocks
- Wiki link support with autocomplete

---

### Tree Navigation

Left sidebar displays hierarchical article tree.

**Features:**
- Lazy loading of children on expand
- Title search with ancestor expansion
- Visual distinction by article type
- Context menu (Add Child, Delete)
- Selection highlighting

**Virtual Groups:**
Worlds display at root level with virtual groups organizing content:
- Campaigns (Campaign entities)
- Player Characters (Character articles)
- Wiki (WikiArticle articles)
- Uncategorized (Legacy articles)

---

### Dashboard

Home page with campaign overview and quick actions.

**Sections:**
- World statistics (article count, campaign count)
- Recent articles
- Quick action buttons
- Inspirational quotes (Quotable API)

**Quick Actions:**
- Create new article
- Create new campaign
- Create new character
- Global search

---

### Public World Sharing

Share your world publicly with anyone via a unique URL.

**How It Works:**
1. Navigate to your World detail page
2. In "Public Sharing" section, toggle "Make this world publicly accessible"
3. Choose a unique public slug (e.g., `forgotten-realms`)
4. Save your changes
5. Share the URL: `https://chronicis.app/w/your-slug`

**Article Visibility:**
Articles have three visibility levels:
- **Public** - Visible to anyone (anonymous access)
- **MembersOnly** - Visible only to authenticated world/campaign members
- **Private** - Visible only to the article creator

**Public Slug Rules:**
- 3-100 characters
- Lowercase letters, numbers, and hyphens only
- No leading or trailing hyphens
- Must be globally unique
- Some slugs are reserved (api, admin, public, private, etc.)

**Public Viewer Features:**
- Anonymous read-only access at `/w/{publicSlug}`
- Article tree sidebar with navigation
- Basic markdown rendering
- Breadcrumb navigation
- Mobile-responsive layout

**Security:**
- Only articles marked as "Public" visibility are shown
- MembersOnly and Private articles are completely hidden (no placeholder)
- No authentication required to view public content
- World owner maintains full control over what's shared

---

## API Reference

### Article Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/articles` | List root articles |
| GET | `/api/articles/{id}` | Get article by ID |
| GET | `/api/articles/path/{path}` | Get article by slug path |
| POST | `/api/articles` | Create article |
| PUT | `/api/articles/{id}` | Update article |
| DELETE | `/api/articles/{id}` | Delete article |
| PATCH | `/api/articles/{id}/parent` | Move article to new parent |
| GET | `/api/articles/{id}/backlinks` | Get backlinks |
| POST | `/api/articles/{id}/summary/generate` | Generate AI summary |
| GET | `/api/articles/search` | Full-text search |

### World Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/worlds` | List user's worlds |
| GET | `/api/worlds/{id}` | Get world details |
| POST | `/api/worlds` | Create world |
| PUT | `/api/worlds/{id}` | Update world |
| DELETE | `/api/worlds/{id}` | Delete world |
| GET | `/api/worlds/{id}/link-suggestions` | Autocomplete suggestions |
| POST | `/api/worlds/{id}/check-public-slug` | Check slug availability |

### Public World Endpoints (Anonymous)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/public/worlds/{publicSlug}` | Get public world |
| GET | `/api/public/worlds/{publicSlug}/articles` | Get public article tree |
| GET | `/api/public/worlds/{publicSlug}/articles/{*path}` | Get public article |

### Campaign Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/campaigns/{id}` | Get campaign details |
| POST | `/api/campaigns` | Create campaign |
| PUT | `/api/campaigns/{id}` | Update campaign |
| DELETE | `/api/campaigns/{id}` | Delete campaign |

### Arc Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/arcs/{id}` | Get arc details |
| POST | `/api/arcs` | Create arc |
| PUT | `/api/arcs/{id}` | Update arc |
| DELETE | `/api/arcs/{id}` | Delete arc |

### Link Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/worlds/{id}/link-suggestions?query=` | Autocomplete |
| POST | `/api/articles/resolve-links` | Resolve link targets |

### Utility Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Health check (anonymous) |

---

## Frontend Services

### API Services

| Service | Purpose |
|---------|---------|
| `ArticleApiService` | Article CRUD, backlinks |
| `WorldApiService` | World management |
| `CampaignApiService` | Campaign operations |
| `ArcApiService` | Arc operations |
| `SearchApiService` | Global search |
| `LinkApiService` | Link suggestions, resolution |
| `AISummaryApiService` | Summary generation |
| `QuoteService` | Inspirational quotes |
| `PublicApiService` | Anonymous public world access |

### State Services

| Service | Purpose |
|---------|---------|
| `TreeStateService` | Tree view state |
| `AppContextService` | Current world/campaign |
| `ArticleCacheService` | Article caching |
| `BreadcrumbService` | Navigation path |
| `WikiLinkService` | Link text parsing |

---

## Companion Applications

### Chronicis.CaptureApp

Windows application for capturing and transcribing D&D session audio.

**Features:**
- System audio capture (WASAPI loopback)
- Real-time transcription using Whisper.NET
- Chunk-based processing (5-20 second segments)
- Markdown transcript output
- System tray integration

**Tech Stack:**
- WinForms with MaterialSkin
- NAudio for audio capture
- Whisper.NET for AI transcription

**Status:** Prototype (not integrated with main app)

---

## Related Documents

- [STATUS.md](STATUS.md) - Project status
- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical architecture
- [CHANGELOG.md](CHANGELOG.md) - Version history
