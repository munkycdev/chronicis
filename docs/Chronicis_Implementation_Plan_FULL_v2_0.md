# Chronicis Implementation Plan - Complete Reference

**Version:** 2.0 | **Date:** December 24, 2025  
**Purpose:** Complete phase-by-phase implementation guide with detailed specifications

**CHANGES IN v2.0:**
- Phase 10: **COMPLETE** - Taxonomy & Entity Management
- New data model: World → Campaign → Arc hierarchy
- Virtual groups in tree navigation (Campaigns, Player Characters, Wiki, Uncategorized)
- Entity detail pages for World, Campaign, and Arc with inline editing
- Creation dialogs for all entity types
- Tree selection/highlighting for all navigable entities
- Drag-and-drop support for moving articles between virtual groups
- ArticleType enum for categorizing articles (WikiArticle, Character, Session, etc.)

---

## Quick Navigation

- [Project Context](#project-context) | [Phase Overview](#phase-overview)
- [Phase 0-8 Summary](#phases-0-8-summary) | [Phase 9](#phase-9) | [Phase 9.5](#phase-9-5) | [Phase 10](#phase-10) | [Phase 11](#phase-11) | [Phase 12](#phase-12) | [Phase 13](#phase-13)
- [Appendices](#appendices)

---

## Project Context

**What:** Web-based knowledge management for D&D campaigns  
**Stack:** Blazor WASM + Azure Functions + Azure SQL + MudBlazor  
**Timeline:** 16 weeks (13 phases)  
**Approach:** Local dev → Test → Deploy to Azure when stable

**Key Specs:**
- Design: `/mnt/project/Chronicis_Style_Guide.pdf`
- Platform: `/mnt/project/ChronicisPlatformSpec_md.pdf`
- Features: `/mnt/project/Chronicis_Feature_Specification.pdf`

**Editing Paradigm:** Inline editing like Obsidian (always-editable fields, auto-save, no modal dialogs)

---

## Phase Overview

| # | Phase | Weeks | Status | Deliverables |
|---|-------|-------|--------|--------------|
| 0 | Infrastructure & Setup | 1 | ✅ Complete | Azure resources, local environment, skeleton app |
| 1 | Data Model & Tree Nav | 2 | ✅ Complete | Article entity, hierarchy, tree view |
| 2 | CRUD Operations & Inline Editing | 1 | ✅ Complete | Create, edit, delete with inline editing |
| 3 | Search & Discovery | 1 | ✅ Complete | Title search, filtering, dedicated API |
| 4 | Markdown & Rich Content | 1 | ✅ Complete | TipTap WYSIWYG editor, rendering |
| 5 | Visual Design & Polish | 1 | ✅ Complete | Style guide, UX, dashboard, routing |
| 6 | Hashtag System | 1 | ✅ Complete | Parsing, visual styling, storage, API |
| 7 | Backlinks & Graph | 1 | ✅ Complete | Backlinks panel, tooltips, navigation, linking UI |
| 8 | AI Summaries | 2 | ✅ Complete | Azure OpenAI integration, summary generation, cost controls |
| 9 | Advanced Search | 1 | ✅ Complete | Full-text content search, grouped results, global UI |
| 9.5 | Auth Architecture | 0.5 | ✅ Complete | Global middleware, centralized HttpClient |
| 10 | Taxonomy & Entities | 1.5 | ✅ **COMPLETE** | World/Campaign/Arc hierarchy, entity pages, virtual groups |
| 11 | Drag & Drop | 1 | 📜 Next | Tree reorganization enhancements |
| 12 | Icons & Polish | 1 | ⏳ Pending | Custom icons, final touches |
| 13 | Testing & Deploy | 2 | ⏳ Pending | E2E tests, optimization, production |

---


<a name="phases-0-8-summary"></a>

## Phases 0-8: Completed Foundation

**Status:** ✅ All Complete

### Phase 0: Infrastructure & Project Setup
- Azure Resource Group, SQL Database, Key Vault, Static Web App
- Local development environment with .NET 9
- Health check endpoints

### Phase 1: Core Data Model & Tree Navigation
- Article entity with self-referencing hierarchy
- Tree view with lazy loading
- Breadcrumb navigation
- GET endpoints for articles, children, and details

### Phase 2: CRUD Operations & Inline Editing
- Always-editable ArticleDetail component
- Auto-save for body (0.5s delay), manual save for title
- Context menu with Add Child and Delete
- POST, PUT, DELETE API endpoints

### Phase 3: Search & Discovery
- Title-only search for tree navigation
- GET /api/articles/search/title endpoint
- Case-insensitive substring matching
- Auto-expand ancestors of matches

### Phase 4: Markdown & Rich Content (WYSIWYG Editor)
- TipTap v3.11.0 integration via CDN
- Real-time WYSIWYG markdown editing
- Custom Chronicis styling for headers, lists, code blocks
- Markdown ↔ HTML conversion

### Phase 5: Visual Design & Polish
- Chronicis theme with beige-gold (#C4AF8E) and deep blue-grey (#1F2A33)
- Enhanced dashboard with stats, recent articles, quick actions
- URL-based routing with slugs (/article/waterdeep)
- Dynamic browser page titles
- Logo navigation to dashboard
- Quotable API integration for inspirational quotes

### Phase 6: Hashtag System Foundation
- TipTap Mark extension for hashtag detection
- Hashtag and ArticleHashtag database tables
- HashtagParser and HashtagSyncService
- Visual styling with beige-gold color
- Auto-sync on article save
- Case-insensitive storage

### Phase 7: Backlinks & Entity Graph
- BacklinksPanel in metadata drawer
- Hashtag hover tooltips with article previews
- Click navigation for linked hashtags
- HashtagLinkDialog for linking unlinked hashtags
- Visual distinction (dotted underline for linked)
- JavaScript ↔ Blazor event communication

### Phase 8: AI Summary Generation
- Azure OpenAI integration with GPT-4.1-mini
- AISummaryService with cost estimation
- Configuration-driven prompts
- Pre-generation cost transparency
- AISummarySection component with collapsible UI
- Copy, regenerate, clear actions
- Application Insights logging

---

<a name="phase-9"></a>

## Phase 9: Advanced Search & Content Discovery

**Status:** ✅ **COMPLETE**

**Goal:** Implement full-text content search across article bodies and hashtags with global search interface

- Global search box in app header
- Search across titles, bodies, and hashtags
- Results grouped by match type with context snippets
- Query term highlighting
- Click-to-navigate to matched articles

---

<a name="phase-9-5"></a>

## Phase 9.5: Authentication Architecture Refactoring

**Status:** ✅ **COMPLETE**

**Goal:** Centralize authentication handling to eliminate repetitive code

- Global authentication middleware for Azure Functions
- `[AllowAnonymous]` attribute for public endpoints
- Centralized HttpClient configuration with automatic token attachment
- Removed redundant base classes and wrappers

---

<a name="phase-10"></a>

## Phase 10: Taxonomy & Entity Management

**Status:** ✅ **COMPLETE** (v2.0)

**Goal:** Implement proper campaign organization with World → Campaign → Arc hierarchy

**Completed:** December 24, 2025

### Data Model

- **World**: Top-level container for all campaign content
- **Campaign**: A specific campaign/adventure within a world
- **Arc**: Story arcs or acts within a campaign (e.g., "Act 1", "Chapter 1")
- **ArticleType**: Enum categorizing articles (WikiArticle, Character, CharacterNote, Session, SessionNote, Legacy)

### Tree Navigation

- Worlds display at root level of navigation tree
- Virtual groups organize content within each world:
  - **Campaigns**: Contains Campaign entities with their Arcs
  - **Player Characters**: Top-level Character articles
  - **Wiki**: Top-level WikiArticle articles  
  - **Uncategorized**: Legacy and untyped articles
- Visual distinction for each node type (icons, styling)
- Selection highlighting for World, Campaign, Arc, and Article nodes

### Entity Detail Pages

- **World Detail** (`/world/{id}`): Edit name/description, quick actions to create campaigns/characters/wiki articles, world statistics
- **Campaign Detail** (`/campaign/{id}`): Edit name/description, list of arcs with session counts, create new arcs
- **Arc Detail** (`/arc/{id}`): Edit name/description/sort order, list of sessions, create new sessions

### Creation Dialogs

- Create World dialog
- Create Campaign dialog (with world selection)
- Create Arc dialog (with sort order)
- Create Article dialog (with type selection)

### Article Organization

- Articles automatically categorized by ArticleType into virtual groups
- Drag-and-drop moves articles between virtual groups (updates ArticleType)
- Sessions belong to Arcs within Campaigns
- Characters and Wiki articles are top-level within their groups

### Routes

- `/world/{worldId}` - World detail page
- `/campaign/{campaignId}` - Campaign detail page
- `/arc/{arcId}` - Arc detail page
- `/article/{path}` - Article detail (unchanged)

### Success Criteria

1. ✅ Worlds contain all campaign content
2. ✅ Virtual groups organize articles by type
3. ✅ Can create and edit Worlds, Campaigns, and Arcs
4. ✅ Tree navigation shows full hierarchy
5. ✅ Entity nodes highlight when selected
6. ✅ Breadcrumb navigation works for all entity types
7. ✅ Drag-and-drop updates article types appropriately

---

<a name="phase-11"></a>

## Phase 11: Drag-and-Drop Enhancements

**Status:** 📜 Next Phase

**Goal:** Enhance drag-and-drop with additional reorganization features

### Planned Features

- Drag articles between parent articles
- Drag to reorder within same parent
- Visual drop indicators
- Undo functionality for moves
- Keyboard accessibility for reorganization

### Success Criteria

1. Can drag article to new parent article
2. Cannot create circular references
3. Clear visual feedback during drag
4. Undo available after move

---

<a name="phase-12"></a>

## Phase 12: Custom Icons & Visual Enhancements

**Status:** ⏳ Pending

**Goal:** Allow custom emoji icons and final polish

### Planned Features

- EmojiPicker component enhancement
- Icon selection in inline editor (near title)
- Icons in breadcrumbs
- Large icon in article header
- Smooth animations throughout
- Enhanced tooltips

### Success Criteria

1. Can select emoji for article
2. Icons display everywhere
3. Can remove icon
4. UI feels polished
5. Works with inline editing workflow

---

<a name="phase-13"></a>

## Phase 13: Testing, Performance & Deployment

**Status:** ⏳ Pending

**Goal:** Ensure quality, optimize, deploy to production

### Testing Strategy

- Unit tests for Article CRUD
- Unit tests for entity services (World, Campaign, Arc)
- Unit tests for hashtag parsing
- Unit tests for AI summary service
- Unit tests for search functionality
- Unit tests for authentication middleware
- Integration tests for API endpoints
- Manual test plan execution

### Performance Optimizations

- Add database indexes
- Query optimization with projections
- Frontend debouncing
- Response compression
- Caching headers

### Deployment Steps

- Validate Azure infrastructure
- Configure GitHub Actions
- Set environment variables
- Run database migrations on Azure SQL
- Smoke test deployed app
- Set up Application Insights
- Configure monitoring and alerts

---


<a name="appendices"></a>

## Appendices

### A. Essential Commands

**Development:**
```powershell
# Run Blazor client with hot reload
cd src\Chronicis.Client
dotnet watch run

# Run Azure Functions
cd src\Chronicis.Api
func start

# EF Migrations
cd src\Chronicis.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

### B. Performance Targets

- **Initial Load:** < 3 seconds
- **Tree Expansion:** < 300ms
- **Article Display:** < 500ms
- **Search Results:** < 1 second
- **Auto-Save:** < 500ms
- **AI Summary Generation:** < 30 seconds
- **Entity Page Load:** < 500ms

### C. Key Routes

| Route | Page | Description |
|-------|------|-------------|
| `/dashboard` | Dashboard | Home page with stats and quick actions |
| `/world/{id}` | WorldDetail | Edit world, create campaigns/characters |
| `/campaign/{id}` | CampaignDetail | Edit campaign, manage arcs |
| `/arc/{id}` | ArcDetail | Edit arc, manage sessions |
| `/article/{path}` | Articles | Article detail with inline editing |
| `/search` | Search | Global search results |

### D. Data Model Summary

```
World (entity)
├── Campaign (entity)
│   └── Arc (entity)
│       └── Article (type: Session)
│           └── Article (type: SessionNote)
├── Article (type: Character)
│   └── Article (type: CharacterNote)
├── Article (type: WikiArticle)
│   └── Article (type: WikiArticle)
└── Article (type: Legacy/uncategorized)
```

### E. Virtual Group Mapping

| Virtual Group | ArticleType | Description |
|---------------|-------------|-------------|
| Campaigns | N/A | Contains Campaign entities |
| Player Characters | Character | Top-level character articles |
| Wiki | WikiArticle | Top-level wiki/lore articles |
| Uncategorized | Legacy | Articles without specific type |

---

## Final Notes

**Current Progress:**
**10 of 13 phases complete** (~77% of project)
- Phases 0-10: ✅ Complete
- Phase 11: 📜 Ready to start (Drag & Drop Enhancements)
- Phases 12-13: ⏳ Pending

**When Ready to Start Phase 11:**
Create a new chat, upload this plan and the spec PDFs, and say:
*"I'm ready to start Phase 11 of Chronicis implementation - Drag & Drop Enhancements. Note: Phases 0-10 are complete including the full taxonomy system with World/Campaign/Arc hierarchy and entity detail pages."*

---

**Version History:**
- 2.0 (2025-12-24): Phase 10 COMPLETE - Taxonomy & Entity Management, World/Campaign/Arc hierarchy, entity pages, virtual groups
- 1.9 (2025-12-01): Phase 9.5 COMPLETE - Auth architecture refactoring
- 1.8 (2025-11-28): Phase 9 COMPLETE - Full-text content search
- 1.7 (2025-11-27): Phase 8 COMPLETE - AI summaries with Azure OpenAI
- 1.6 (2025-11-27): Phase 7 COMPLETE - Interactive hashtags, backlinks
- 1.5 (2025-11-26): Phase 6 COMPLETE - Full hashtag system
- 1.4 (2025-11-25): Phase 5 COMPLETE - Dashboard, routing
- Earlier versions: Initial phases

---

*End of Implementation Plan*
