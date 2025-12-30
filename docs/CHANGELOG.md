# Chronicis - Changelog

All notable changes to this project are documented in this file.

---

## [Unreleased]

### Planned
- Phase 11: Drag & Drop Enhancements
- Phase 12: Icons & Polish
- Phase 13: Testing & Deployment

---

## [2.0.0] - 2025-12-24

### Phase 10: Taxonomy & Entity Management

**Added:**
- World entity as top-level content container
- Campaign entity for gaming group spaces
- Arc entity for story organization
- ArticleType enum (WikiArticle, Character, CharacterNote, Session, SessionNote, Legacy)
- ArticleVisibility enum (Public, Private)
- CampaignRole enum (GameMaster, Player, Observer)
- Virtual groups in tree navigation (Campaigns, Characters, Wiki, Uncategorized)
- World detail page (`/world/{id}`) with inline editing
- Campaign detail page (`/campaign/{id}`) with arc management
- Arc detail page (`/arc/{id}`) with session list
- Creation dialogs for World, Campaign, Arc, and Article
- Automatic default World creation for new users

**Changed:**
- Tree navigation now displays Worlds at root level
- Articles organized by ArticleType into virtual groups
- Breadcrumb navigation supports all entity types

---

## [1.9.5] - 2025-12-23

### Wiki Links System (Replaced Hashtags)

**Added:**
- Wiki-style `[[Article Name]]` link syntax
- ArticleLink entity for link storage
- Link parser service for extracting links from body
- Link sync service for maintaining ArticleLink table
- Autocomplete for link suggestions (triggers after `[[` + 3 chars)
- Create article flow from autocomplete
- Broken link detection and visual indicator
- Link resolution API for eager loading
- Backlinks panel updated for wiki links

**Removed:**
- Hashtag system (Hashtag entity, ArticleHashtag, HashtagParser, etc.)
- `#entity` syntax support
- Hashtag-related TipTap extensions

**Changed:**
- AI summary service now queries ArticleLink instead of hashtags
- Backlinks computed from ArticleLink.TargetArticleId

---

## [1.9.0] - 2025-12-01

### Phase 9.5: Authentication Architecture Refactoring

**Added:**
- `AuthenticationMiddleware` for global JWT validation (Azure Functions)
- `AllowAnonymousAttribute` for public endpoints
- `FunctionContextExtensions` with `GetUser()` and `GetRequiredUser()`
- `AuthorizationMessageHandler` for automatic token attachment (Blazor)
- Named HttpClient "ChronicisApi" with IHttpClientFactory pattern

**Removed:**
- `BaseAuthenticatedFunction` base class
- `ArticleBaseClass` base class
- `AuthHttpClient` wrapper class
- Per-function authentication code

**Changed:**
- All API services now use centralized HttpClient configuration
- Functions access user via `context.GetRequiredUser()`

---

## [1.8.0] - 2025-11-28

### Phase 9: Advanced Search & Content Discovery

**Added:**
- Global search box in app header
- Full-text search across titles, bodies, and hashtags
- Search results page with grouped results
- SearchResultCard component with term highlighting
- SearchApiService for API communication
- Context snippets (200 characters) showing search term

**API:**
- `GET /api/articles/search?query={term}` - Returns grouped results

---

## [1.7.0] - 2025-11-27

### Phase 8: AI Summary Generation

**Added:**
- Azure OpenAI integration (GPT-4.1-mini)
- AISummaryService with cost estimation
- Configuration-driven prompts
- AISummarySection component (collapsible UI)
- Copy, regenerate, clear actions
- Application Insights logging
- Pre-generation cost transparency

**API:**
- `POST /api/articles/{id}/summary/generate`
- `GET /api/articles/{id}/summary`

---

## [1.6.0] - 2025-11-27

### Phase 7: Backlinks & Entity Graph

**Added:**
- BacklinksPanel in metadata drawer
- Hashtag hover tooltips with article previews
- Click navigation for linked hashtags
- HashtagLinkDialog for linking unlinked hashtags
- Visual distinction (dotted underline for linked)
- JavaScript ↔ Blazor event communication

**API:**
- `GET /api/articles/{id}/backlinks`
- `GET /api/hashtags/{name}/preview`

---

## [1.5.0] - 2025-11-26

### Phase 6: Hashtag System

**Added:**
- TipTap Mark extension for hashtag detection
- Hashtag and ArticleHashtag database tables
- HashtagParser service
- HashtagSyncService for auto-sync on save
- Visual styling with beige-gold color
- Case-insensitive hashtag storage

---

## [1.4.0] - 2025-11-25

### Phase 5: Visual Design & Polish

**Added:**
- Chronicis theme (beige-gold #C4AF8E, deep blue-grey #1F2A33)
- Enhanced dashboard with stats, recent articles, quick actions
- URL-based routing with slugs (`/article/waterdeep`)
- Dynamic browser page titles
- Logo navigation to dashboard
- Quotable API integration for inspirational quotes

---

## [1.3.0] - 2025-11-24

### Phase 4: Markdown & Rich Content

**Added:**
- TipTap v3.11.0 integration via CDN
- Real-time WYSIWYG markdown editing
- Custom Chronicis styling for headers, lists, code blocks
- Markdown ↔ HTML conversion
- tipTapIntegration.js for editor lifecycle

---

## [1.2.0] - 2025-11-23

### Phase 3: Search & Discovery

**Added:**
- Title-only search for tree navigation
- GET /api/articles/search/title endpoint
- Case-insensitive substring matching
- Auto-expand ancestors of search matches

---

## [1.1.0] - 2025-11-22

### Phase 2: CRUD Operations & Inline Editing

**Added:**
- Always-editable ArticleDetail component
- Auto-save for body (0.5s debounce)
- Manual save for title (blur/Enter)
- Context menu with Add Child and Delete
- Toast notifications for save status

**API:**
- `POST /api/articles`
- `PUT /api/articles/{id}`
- `DELETE /api/articles/{id}`

---

## [1.0.0] - 2025-11-20

### Phase 0-1: Infrastructure & Core Data Model

**Added:**
- Azure Resource Group, SQL Database, Key Vault, Static Web App
- Local development environment with .NET 9
- Article entity with self-referencing hierarchy
- Tree view with lazy loading
- Breadcrumb navigation
- Health check endpoints

**API:**
- `GET /api/articles` - Root articles
- `GET /api/articles/{id}` - Article details
- `GET /api/articles/{id}/children` - Child articles

---

## Version Naming

- **Major:** Breaking changes or significant feature sets
- **Minor:** New features or significant enhancements
- **Patch:** Bug fixes and minor improvements

---

## Related Documents

- [STATUS.md](STATUS.md) - Project status
- [ARCHITECTURE.md](ARCHITECTURE.md) - Technical architecture
- [FEATURES.md](FEATURES.md) - Feature documentation
