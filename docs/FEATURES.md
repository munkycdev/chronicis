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

### External Knowledge Links

Chronicis supports linking to external reference sources using the wiki-link workflow.

**Syntax:**
- Trigger autocomplete with: `[[sourceKey/`
- External token format: `[[source|id|title]]`

**Example:**
- Typing `[[srd/acid` shows SRD suggestions
- Selecting an entry inserts an external link chip

**Preview:**
- Clicking an external chip opens an in-app preview drawer
- Preview content is fetched live from the provider and rendered as Markdown
- A link to the source site is available when provided by the provider

**Extensibility:**
- Providers are keyed by a short prefix (example: `srd`, future: `kobold`)
- Additional providers can be added without changing the editor token format

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
