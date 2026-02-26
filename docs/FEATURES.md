# Chronicis - Feature Documentation

**Last Updated:** February 26, 2026

> **Note on Terminology:** This document uses vocabulary terms defined in [Vocabulary.md](Vocabulary.md). Key concepts include **WikiLinks** (internal article-to-article references), **ExternalReferences** (embedded third-party D&D content), and **WorldBookmarks** (user-saved external URLs). See Vocabulary.md for complete definitions.

---

## Core Features

### Hierarchical Article Organization

Articles can be nested infinitely deep to mirror campaign structure. Each article belongs to a World and can have a parent article and any number of children.

**Article Types:**
- **WikiArticle** - General content (locations, NPCs, items, lore)
- **Character** - Player or NPC character (top-level in Characters group)
- **CharacterNote** - Notes nested under a Character
- **SessionNote** - A player or GM's notes for a specific Session (see [Multi-Author Session Notes](#multi-author-session-notes))
- **Legacy** - Unmigrated content from old schema

> **Note:** `Session` is a first-class domain entity (not an article type). Sessions belong to Arcs and contain `SessionNote` articles authored by individual players.

**Virtual Groups in Tree:**
- **Campaigns** - Campaign entities with their Arcs
- **Player Characters** - Top-level Character articles
- **Wiki** - Top-level WikiArticle articles
- **Uncategorized** - Legacy and untyped articles

---

### WikiLinks (Internal Article References)

**WikiLinks** enable internal article-to-article references using `[[Article Name]]` syntax, similar to Obsidian.

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
- ArticleLink table tracks all WikiLinks for backlink queries

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

### ExternalReferences (Third-Party D&D Content)

**ExternalReferences** enable embedded references to third-party D&D content (SRD, Open5e) using the wiki-link workflow. Multiple providers are available, including live API integration and blob-backed local SRD data.

**Available Providers:**

| Provider | Trigger | Source | Description |
|----------|---------|--------|-------------|
| Open5e | `[[srd/` | Live API | D&D 5e SRD via Open5e API (10 categories) |
| SRD 2014 | `[[srd14/` | Blob Storage | D&D 5e 2014 SRD (JSON in Azure Blob) |
| SRD 2024 | `[[srd24/` | Blob Storage | D&D 5e 2024 SRD (JSON in Azure Blob) |

**Syntax:**
- Trigger autocomplete with provider key: `[[srd/`, `[[srd14/`, or `[[srd24/`
- Category browsing: `[[srd14/spells/` shows spell category
- Search within category: `[[srd14/spells/fire` searches for spells containing "fire"
- Cross-category search: `[[srd14/fire` searches across all categories
- Hierarchical navigation: `[[srd14/items/armor/lea` finds armor starting with "lea"
- External token format: `[[source|id|title]]`

**Open5e Provider (Live API):**

The Open5e provider queries the Open5e API in real-time for D&D 5e content.

| Category | Description | API Endpoint |
|----------|-------------|--------------|
| `spells` | D&D 5e Spells | `/v2/spells` |
| `monsters` | Creatures & Monsters | `/v2/creatures` |
| `magicitems` | Magic Items | `/v2/items` |
| `conditions` | Status Conditions | `/v2/conditions` |
| `backgrounds` | Character Backgrounds | `/v2/backgrounds` |
| `feats` | Feats | `/v2/feats` |
| `classes` | Character Classes | `/v2/classes` |
| `races` | Playable Races | `/v2/races` |
| `weapons` | Weapons | `/v2/weapons` |
| `armor` | Armor | `/v2/armor` |

**Blob-Backed SRD Providers:**

The `srd14` and `srd24` providers use normalized JSON data stored in Azure Blob Storage, providing:
- Instant category browsing (no API latency)
- Hierarchical category structure (items/armor/heavy, items/weapons/martial/melee)
- Cross-category search when no category specified
- ~300x faster index building (uses filenames instead of downloading content)
- Offline-capable architecture (no external API dependency)

**Workflow:**
1. Type `[[srd/` (or `[[srd14/`, `[[srd24/`) to see category suggestions with icons
2. Select a category (e.g., "✨ Spells") to browse that category
3. Continue typing to search (e.g., `[[srd14/spells/fire`)
4. Or search across all categories (e.g., `[[srd14/fire`)
5. Select an entry to insert an external link chip

**Preview Drawer:**
- Click any external link chip to open a styled preview drawer
- Content rendered as markdown with Chronicis styling
- Styling matches the app's metadata sidebar (soft off-white background, dark blue headers)
- Provider badge shows source (Open5e, SRD 2014, SRD 2024)

**External Link Metadata:**
- Articles display "External Resources" panel showing all external links used
- Grouped by provider with chip styling
- Deep blue-grey chips with beige-gold provider badges
- Click to open preview drawer from metadata panel
- Attribution and source links included

**Technical Details:**

*Open5e Provider:*
- Provider: Open5e API (https://api.open5e.com)
- API Version: v2 exclusively
- Document filter: `document__gamesystem__key=a5e` (SRD content)
- Name-based search: Uses `name__contains` parameter with client-side filtering

*Blob-Backed Providers (SRD 2014/2024):*
- Storage: Azure Blob Storage with normalized JSON files
- Structure: One JSON file per entity with hierarchical folder organization
- Indexing: Filename-based for ~300x faster startup (no file downloads required)
- Categories: Hierarchical structure (e.g., items/armor/heavy, items/weapons/martial/melee)
- Search: Cross-category search and category-specific search supported
- Performance: <100ms for autocomplete, instant category browsing

**Extensibility:**
- Providers are keyed by a short prefix (e.g., `srd`, `srd14`, `srd24`)
- Additional providers can be added by implementing `IExternalLinkProvider`
- Provider architecture supports future sources (Kobold Press, homebrew APIs, etc.)
- Blob-backed pattern enables offline-capable reference data

---

### Multi-Author Session Notes

Sessions are first-class domain entities belonging to Arcs. Each session contains one or more `SessionNote` articles authored by individual world members.

**How It Works:**
1. GM creates a session on the Arc Detail page
2. A default `SessionNote` is automatically created for the creator
3. Any world member navigates to the Session Detail page and clicks "Add Session Note" to contribute their own note
4. Each note is its own article with the full TipTap editor, wiki links, external references, and privacy toggle
5. The AI Summary on Session Detail aggregates content from all `SessionNote` records for that session to produce a multi-perspective summary

**Features:**
- Each player's perspective is captured independently in their own note
- Private session notes are supported — GMs or players can mark their note private to keep planning or personal thoughts hidden
- Notes list on Session Detail shows title, author, and visibility at a glance
- AI summary combines all notes, giving the GM a single cohesive summary of everything recorded

---

### Tutorial World

New users are immediately given a fully populated example world to explore rather than starting from scratch.

**How It Works:**
1. Sysadmin maintains a canonical tutorial world with rich pre-populated articles, campaigns, arcs, and sessions
2. When a new user logs in for the first time, the canonical world is cloned for them
3. The cloned world is private to that user and fully editable — it is their own copy to explore and modify
4. Future improvements to the sysadmin canonical world become the baseline for all subsequent new-user clones; existing users keep their current tutorial world unchanged

**Purpose:**
- Demonstrates all major features in context (wiki articles, linked characters, session notes, quests, backlinks)
- Gives new users something concrete to interact with before building their own content
- Reduces the "blank page" problem for first-time DMs and players

**Technical Details:**
- Clone operation copies the full world hierarchy: world metadata, all articles, campaigns, arcs, sessions, and session notes
- `SysAdminTutorialsController` provides the admin endpoint for managing the canonical source world
- Clone is triggered on first login via user provisioning logic

---

### Contextual Help Sidebar

A dedicated help panel that surfaces page-specific guidance wherever you are in the app.

**How It Works:**
1. The Tutorial Drawer sits alongside the metadata and quest drawers in the right panel area
2. Open it via the help icon in the app bar or the drawer coordinator
3. Content is resolved automatically based on the current page type and article type
4. Sysadmins author and update tutorial content via the Admin panel; content is stored as `Tutorial` entities

**Supported Page Types:**
- `world-detail` — World overview, campaigns, and settings guidance
- `campaign-detail` — Campaign management and arc organization
- `arc-detail` — Arc structure, session creation, quest tracking
- `session-detail` — Session workflow, adding notes, AI summary
- `session-note` — Writing notes, wiki links, private vs. public content
- `player-character` — Character articles, claiming, notes
- `wiki` — Article hierarchy, linking, AI summaries

**Tutorial World Behavior:**
- When the user is viewing content from the tutorial world, the drawer is pinned open (`IsForcedOpen = true`) and the close button is suppressed
- This ensures new users always have guidance visible while exploring the example content

**Relationship to GettingStarted Wizard:**
- The existing `/getting-started` wizard is retained as a high-level orientation to Chronicis's organizational structure
- The final step of the wizard now mentions the tutorial sidebar so users know it exists
- The sidebar provides the deeper, context-specific guidance the wizard does not

**Technical Details:**
- `TutorialDrawer` component resolves content via `TutorialPageTypeResolver` on every navigation event
- `ITutorialApiService` / `TutorialApiService` handle client-side content retrieval
- `DrawerCoordinator` manages open/close state across all right-side drawers

---

### GM Private Planning Notes

DMs and GMs have a dedicated private notes area on every major entity page for session prep and planning content that players should never see.

**Where Available:**
- World Detail page
- Campaign Detail page
- Arc Detail page
- Session Detail page

**How It Works:**
- Each of the above entities has a `PrivateNotes` field stored directly on the record
- A "Private Notes" tab is rendered on the detail page, but only for users with the GM role
- Non-GM world members cannot see the tab or its contents — the tab is not rendered at all for them
- GMs can additionally create private `SessionNote` articles (using the existing article privacy toggle) for more structured per-session planning documents

**Use Cases:**
- Pre-session encounter prep hidden from players
- Secret NPC motivations and plot twists at the campaign or arc level
- World-level lore that hasn't been revealed yet
- Quick reminders and to-do lists for the GM within the session workflow

---

### Shared Quests Log

**How It Works**
1. GM creates quests on the Arc Detail page
2. Users hit Ctrl+Q on a session notes page to view the quests sidebar
3. Users can add notes specifically relating to displayed Quests

**Features**
- Quest notes can optionally be associated with session Notes
- Quest note entry uses the same rich markdown editor as elsewhere
- Quests may be marked complete by the GM
- Quest notes append to the list, old notes are never deleted

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
**Features:** WorldBookmarks (quick links to Roll20, D&D Beyond, Discord, etc.)

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
- Inline image upload (drag-and-drop, paste, toolbar button)

---

### Inline Article Images

Upload images directly into article content via drag-and-drop, clipboard paste, or the toolbar image button.

**How It Works:**
1. Drop, paste, or pick an image file while editing an article
2. Image uploads to Azure Blob Storage via SAS URL (same pipeline as document storage)
3. A stable `chronicis-image:{documentId}` reference is stored in the article HTML
4. On render, the reference is resolved to a fresh SAS URL via an authenticated API call
5. SAS URLs are cached in-memory for the session to avoid redundant lookups

**Supported Formats:** PNG, JPEG, GIF, WebP (max 10 MB)

**Storage:**
- Images are `WorldDocument` records with an `ArticleId` FK linking them to their article
- Inline images are excluded from the treeview's External Resources section
- Inline images remain visible in the campaign's document management page
- Deleting an article automatically cleans up its associated images (blobs and DB records)

**Technical Details:**
- TipTap `@tiptap/extension-image` renders proper ProseMirror image nodes
- `imageUpload.js` handles all three upload entry points with client-side validation
- `resolveEditorImages()` runs on editor init to resolve stored references
- `ImagesController` provides an authenticated proxy endpoint for image access

**Keyboard Shortcuts:**
- **Ctrl+S**: Save current article from anywhere in the app
- **Ctrl+N**: Create sibling article (inherits WorldId, CampaignId, Type from current article)
- Shortcuts work while typing in the editor
- Service-based communication between layout and article components for cross-component coordination

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

### Document Storage

Upload and manage documents within your worlds for easy reference and sharing.

**How It Works:**
1. Navigate to your World detail page
2. Click the "Documents" tab
3. Upload files using drag-and-drop or file picker
4. Documents are stored in Azure Blob Storage with world-level isolation

**Supported Features:**
- **Upload**: PDFs, images, and other file types
- **Download**: Direct browser downloads via SAS URLs (no API streaming)
- **Delete**: Remove documents from storage
- **Visibility**: Documents respect world membership (only world members can access)
- **Metadata**: Tracks filename, size, content type, and upload timestamp

**Storage Architecture:**
- Azure Blob Storage with container per world (`world-{worldId}-documents`)
- SAS URL generation for secure, time-limited direct downloads
- No API streaming - browsers download directly from blob storage
- Automatic cleanup when worlds are deleted

**Technical Details:**
- Storage Account: `stchronicis` in `rg-chronicis` resource group
- SAS Token Duration: 1 hour read-only access
- Maximum File Size: Configured at blob storage level
- Blob Naming: `{timestamp}_{filename}` for uniqueness

**API Endpoints:**
- `POST /api/worlds/{worldId}/documents` - Upload document
- `GET /api/worlds/{worldId}/documents` - List documents
- `GET /api/worlds/{worldId}/documents/{documentId}` - Get document metadata with download URL
- `DELETE /api/worlds/{worldId}/documents/{documentId}` - Delete document

---

### World Export

Export your entire world to a downloadable zip archive containing organized Markdown files.

**How It Works:**
1. Navigate to Settings → Data tab
2. Select the world to export
3. Click "Export to Markdown"
4. Browser downloads a zip file with all content

**Export Contents:**
- All articles organized by type and hierarchy
- YAML frontmatter with metadata (title, type, visibility, dates, icon)
- AI summaries included at the end of each file
- Campaigns and Arcs with their sessions
- Wiki links converted to `[[Article Name]]` format

**Folder Structure:**
```
WorldName/
├── Wiki/
│   └── [hierarchical article folders]
├── Characters/
│   └── [character article folders]
└── Campaigns/
    └── CampaignName/
        ├── CampaignName.md
        └── ArcName/
            ├── ArcName.md
            └── SessionName/
                └── SessionName.md
```

**Article File Format:**
```markdown
---
title: "Article Title"
type: WikiArticle
visibility: Public
created: 2025-12-15 10:30:00
modified: 2025-12-20 14:22:00
icon: "🏰"
---

# Article Title

[Article content in Markdown...]

---

## AI Summary

[AI-generated summary if available]

*Generated: 2025-12-18 09:15:00*
```

**Use Cases:**
- Backup campaign data locally
- Migrate to Obsidian, Notion, or other tools
- Share with players who prefer offline access
- Archive completed campaigns

**API:** `GET /api/worlds/{worldId}/export`

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
| GET | `/api/worlds/{id}/export` | Export world to markdown zip |
| POST | `/api/worlds/{id}/documents` | Upload document to world |
| GET | `/api/worlds/{id}/documents` | List world documents |
| GET | `/api/worlds/{id}/documents/{documentId}` | Get document with download URL |
| DELETE | `/api/worlds/{id}/documents/{documentId}` | Delete document |

### Image Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/images/{documentId}` | Authenticated image proxy (302 redirect to SAS URL) |

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

### External Link Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/external-links/suggestions?source=&query=` | Autocomplete suggestions for external providers |
| GET | `/api/external-links/content?source=&id=` | Fetch external content preview as Markdown |


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
| `ExternalLinkApiService` | External link suggestions and preview content |

### State Services

| Service | Purpose |
|---------|---------|
| `TreeStateService` | Tree view state |
| `AppContextService` | Current world/campaign |
| `ArticleCacheService` | Article caching |
| `BreadcrumbService` | Navigation path |
| `WikiLinkService` | Link text parsing |

### Performance Optimizations

**Lazy Loading for Metadata Panels:**
- Backlinks, outgoing links, and AI summary panels use lazy loading
- Data only fetched when panels are opened by the user
- Prevents unnecessary API calls during auto-save and typing
- Panel data cleared when closed to ensure freshness on reopen

**Auto-Save Optimization:**
- Auto-save only triggers panel refresh when metadata drawer is open
- Eliminates excessive API calls during typing when drawer is closed
- Maintains data freshness for users actively viewing metadata

**Autocomplete Positioning:**
- Wiki-link and external-link autocomplete use `position: fixed` with viewport-relative coordinates
- Viewport boundary detection prevents overflow (flips above cursor when near bottom)
- Scroll event handlers automatically hide popups during scrolling
- Padding from viewport edges ensures full visibility

**Blob-Backed Provider Performance:**
- SRD providers use filename-based indexing (no file downloads during startup)
- ~300x faster index building compared to content-based indexing
- Category structure cached for instant browsing
- Cross-category search optimized with pre-built indexes

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

## Future Features

### Knowledge Base Q&A (RAG)

AI-powered conversational interface for querying campaign knowledge. Users can ask natural language questions and receive answers grounded in their World's content.

**Evolution of AI Features:**

Chronicis takes a progressive approach to AI capabilities:

| Stage | Feature | Analogy | Trade-offs |
|-------|---------|---------|------------|
| Current | AI Summary Generation | Flintstones car | Functional but requires user to manually trigger; analyzes only backlinks |
| Next | Self-built RAG (Qdrant + Azure OpenAI) | Honda Civic | Efficient and reliable; full semantic search across all content |
| Future | Azure AI Search integration | Ferrari | Premium experience with hybrid search, document cracking, and advanced relevance tuning |

The self-built RAG approach provides the best balance of capability and cost for the current stage of the product. Migration to Azure AI Search becomes justified when document volume, query complexity, or user expectations exceed what the simpler stack can deliver.

**User Experience:**
- "Ask Your Campaign" interface accessible from World dashboard
- Natural language queries: "What do we know about the Blackstaff?"
- Responses cite source articles with links
- Follow-up questions maintain conversation context

**Supported Content Sources:**
- All articles within the World
- Uploaded documents (PDFs, text files)
- Linked external resources (future)

**How It Works:**
1. Content is chunked and converted to vector embeddings at save/upload time
2. User's question is converted to an embedding
3. Vector similarity search retrieves relevant content chunks
4. Retrieved context + question sent to LLM for response generation
5. Response includes citations to source articles/documents

**Technical Architecture:**

| Component | Technology | Purpose |
|-----------|------------|----------|
| Vector Store | Qdrant Cloud (free tier) | Semantic similarity search |
| Embeddings | Azure OpenAI `text-embedding-3-small` | Convert text to vectors |
| LLM | Azure OpenAI GPT-4o | Generate grounded responses |
| Sync Pipeline | Azure Functions | Keep vector index in sync with SQL |
| Document Storage | Azure Blob Storage | Store uploaded PDFs and files |

**Why This Stack:**
- **Qdrant Cloud free tier:** 1GB storage (~1M vectors), managed service, no infrastructure overhead
- **Azure OpenAI:** Already in use for AI summaries; consistent billing and access patterns
- **SQL remains source of truth:** Vector index is a read-optimized projection, not primary storage

**Chunking Strategy:**
- Articles: One chunk per article (preserve context)
- Long articles: Split at heading boundaries with overlap
- Documents: Semantic chunking with ~500 token target
- Metadata preserved: Article ID, World ID, visibility, timestamps

**Privacy & Scoping:**
- Queries scoped to user's accessible content only
- Private articles excluded from other users' searches
- World-level isolation (no cross-world retrieval)
- Visibility filters applied at query time

**Cost Structure:**
- Embedding costs: ~$0.02 per 1M tokens (negligible)
- LLM response costs: ~$0.01-0.02 per query (primary cost driver)
- Vector storage: $0 (Qdrant free tier)
- Estimated monthly cost at scale: $20-50 for active usage

**Cost Controls:**
- Per-user daily query limits
- Token usage tracking and display
- Rate limiting on API endpoints
- Monitoring and alerting on API spend

**Implementation Phases:**

| Phase | Scope | Dependencies |
|-------|-------|-------------|
| Phase 1 | Article-only RAG | Qdrant setup, embedding pipeline, basic UI |
| Phase 2 | Document upload and ingestion | Blob storage, text extraction, chunking |
| Phase 3 | Conversation history | Context management, follow-up questions |
| Phase 4 | Proactive suggestions | "You might also want to know..." |

---

### Document Library

Upload and manage reference documents within a World.

**Supported Formats:**
- PDF documents
- Plain text files
- Markdown files
- Word documents (future)

**Features:**
- Document viewer with page navigation
- Full-text search within documents
- Automatic integration with RAG pipeline
- Document-level access controls (Public, MembersOnly, Private)

**Storage:** Azure Blob Storage with World-level containers

**Processing Pipeline:**
1. Upload to Blob Storage
2. Text extraction (PDF parsing, encoding detection)
3. Chunking with metadata preservation
4. Embedding generation via Azure OpenAI
5. Vector index update in Qdrant

**Sync Considerations:**
- Documents are immutable after upload (no edit sync needed)
- Deletion triggers vector cleanup
- Re-processing available for failed extractions

---

### Azure AI Search Migration Path

When usage patterns justify the investment (~$75/month Basic tier), migrating to Azure AI Search provides:

**Additional Capabilities:**
- Hybrid search (keyword + semantic in single query)
- Built-in document cracking (PDF, Office, images via OCR)
- Relevance tuning without code changes (scoring profiles)
- Semantic ranking for improved result quality
- Faceted navigation (filter by article type, campaign, date)

**Migration Trigger Points:**
- Document volume exceeds Qdrant free tier (1GB / ~1M vectors)
- Need for advanced document format support (scanned PDFs, Office docs)
- User feedback indicates search relevance issues
- Requirement for hybrid keyword + semantic search

**Migration Approach:**
- SQL remains source of truth (no data model changes)
- Swap Qdrant client for Azure AI Search client
- Rebuild index using Azure AI Search indexers
- Update chunking to leverage built-in skillsets

---

## Related Documents

- [STATUS.md](STATUS.md) - Project status
- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical architecture
- [CHANGELOG.md](CHANGELOG.md) - Version history
