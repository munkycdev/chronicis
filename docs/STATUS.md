# Chronicis - Project Status

**Last Updated:** February 13, 2026  
**Project Phase:** Inline Article Images Complete

---

## Current State

Chronicis is feature-complete for core world management functionality and has undergone a major internal consolidation effort to improve maintainability and clarity.

---

## Architecture & Maintainability Improvements (2026 Q1)

Completed:

- Centralized hierarchy logic into IArticleHierarchyService
- Decomposed TreeStateService into focused internal components
- Split WorldService into core CRUD + specialized membership and sharing services
- Consolidated external link wrappers into IExternalLinkService
- Canonicalized EF migrations directory
- Reduced client Program.cs from 323 lines to 31 lines
- Established canonical vocabulary for linking concepts

Net impact:

- 340+ lines of duplicated logic removed
- 118+ new unit tests added
- Significant reduction in architectural ambiguity
- Zero breaking API changes

---

### What's Working

**Core Functionality:**
- Hierarchical article organization with infinite nesting
- Tree navigation with lazy loading and search
- Inline WYSIWYG editing with TipTap (HTML storage for proper nested list support)
- Auto-save on content changes (0.5s debounce)
- Wiki-style links using `[[Article Name]]` syntax
- Backlinks panel showing articles that reference current article
- AI-powered summary generation using Azure OpenAI
- Full-text search across titles, bodies, and links
- URL routing with slugs for bookmarking
- Drag & drop article reorganization

**Entity Management:**
- World → Campaign → Arc → Session hierarchy
- Virtual groups for article organization (Characters, Wiki, Uncategorized)
- Entity detail pages for Worlds, Campaigns, and Arcs
- Creation dialogs for all entity types
- Article type categorization (WikiArticle, Character, Session, etc.)
- Character claiming system

**Multi-User Collaboration:**
- World-level membership (all campaigns in a world share members)
- Invitation codes in XXXX-XXXX format for easy sharing
- Role-based access (GM, Player, Observer)
- Member management UI (view, change roles, remove)
- Join world flow from Dashboard
- New users start with empty dashboard (no auto-created world)
- Private articles visible only to their creator

**Private Articles:**
- Toggle article privacy from the metadata drawer (right panel)
- Only article creators can mark their articles as private
- Private articles show a lock icon in the tree view (replaces normal icon)
- Lock icon updates immediately without tree reload
- Private articles filtered from other users' views

**Public Sharing:**
- Public world toggle with globally unique slugs
- Three-tier article visibility (Public, MembersOnly, Private)
- Anonymous read-only access at `/w/{publicSlug}`
- Public article tree with navigation
- Public slug availability checking with suggestions
- Copy-to-clipboard for public URLs

**Export & Settings:**
- Export world data to Markdown zip archive
- Settings page with Profile, Data, and Preferences tabs
- YAML frontmatter in exported files (title, type, visibility, dates, icon)
- AI summaries included in exported markdown files
- Folder structure matching tree hierarchy
- Nested list support in HTML→Markdown conversion for export

**Dashboard:**
- Hero section with gradient background and welcome message
- "Create New World" and "Join a World" action buttons
- Contextual server-generated prompts based on user state
- World-centric panels with expandable content
- Active campaign highlighting with session stats
- Claimed characters display with click-to-navigate
- Stats panel showing chronicle totals

**Infrastructure:**
- Auth0 authentication (Discord and Google OAuth)
- ASP.NET Core Web API backend hosted on Azure Container Apps (`ca-chronicis-api`, `api.chronicis.app`)
- Blazor WebAssembly client hosted on Azure Container Apps (`ca-chronicis-client`, `chronicis.app`)
- Azure SQL Database with Entity Framework Core
- Azure Blob Storage for document attachments
- Centralized HttpClient with automatic token attachment
- DataDog APM with in-image agent (direct-to-cloud traces and logs)

**External Knowledge Links:**
- External wiki-style links using `[[srd/` or `[[srd14/` autocomplete triggers
- Open5e API integration with 10 SRD categories (spells, monsters, magic items, conditions, backgrounds, feats, classes, races, weapons, armor)
- Blob-backed D&D SRD providers (2014 and 2024 editions) with JSON data stored in Azure Blob Storage
- Category selection with icons when typing `[[srd/` or `[[srd14/`
- Cross-category search when no slash is present (e.g., `[[srd/fire` searches all categories)
- Hierarchical category navigation (e.g., `[[srd14/items/armor/lea` for armor starting with "lea")
- Search within categories (e.g., `[[srd/spells/fire` for fire spells)
- External link tokens stored as `[[source|id|title]]`
- In-app preview drawer with styled markdown content (soft off-white background, dark blue headers)
- External link chips with provider badges showing source (deep blue-grey with beige-gold accent)
- Metadata panel showing external resources used in each article
- Provider-based architecture for adding additional sources in the future

**Document Storage:**
- Upload documents to worlds (PDFs, images, etc.)
- Azure Blob Storage integration with world-level containers
- Document management UI with upload/download/delete
- SAS URL-based downloads (direct browser access, no API streaming)
- Document visibility controls (Public, MembersOnly, Private)
- Document metadata tracking (filename, size, content type, upload date)

**Inline Article Images:**
- Drag-and-drop, paste, or toolbar-based image upload into article content
- Images stored in Azure Blob Storage and linked to articles via WorldDocument.ArticleId
- Stable `chronicis-image:{documentId}` references in article HTML (never expire)
- Client-side SAS URL resolution on render with in-memory caching
- Automatic image cleanup on article deletion (blobs + DB records)
- Inline images hidden from treeview External Resources (visible in campaign document list)
- Supported formats: PNG, JPEG, GIF, WebP (max 10 MB)

**Keyboard Shortcuts:**
- Ctrl+S: Save current article from anywhere in the app
- Ctrl+N: Create sibling article (inherits context from current article)
- Works while typing in TipTap editor
- Service-based communication between layout and article components

**Performance Optimizations:**
- Lazy loading for metadata panels (backlinks, outgoing links, AI summary estimates)
- Panels only fetch data when opened, reducing unnecessary API calls
- Auto-save only refreshes panels when metadata drawer is open
- Fixed positioning for autocomplete popups with viewport boundary detection

---

## Next Steps

### Optional Enhancements

- Advanced collaboration features
- Audio capture integration

---

## Environment Setup

**Prerequisites:**
- .NET 9 SDK
- Azure Functions Core Tools
- Visual Studio 2022 or VS Code
- SQL Server (LocalDB, Express, or Docker)

**Running Locally:**

```powershell
# Run API
cd src\Chronicis.Api
func start

# Run Client (separate terminal)
cd src\Chronicis.Client
dotnet watch run
```

**Database Migrations:**

```powershell
cd src\Chronicis.Api
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## Related Documents

- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical architecture
- [FEATURES.md](FEATURES.md) - Feature documentation
- [CHANGELOG.md](CHANGELOG.md) - Version history
- [Feature Ideas.md](Feature%20Ideas.md) - Backlog and bug list
