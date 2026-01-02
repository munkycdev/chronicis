# Chronicis - Changelog

All notable changes to this project are documented in this file.

---

## [Unreleased]

### Planned
- Phase 11: Icons & Polish
- Phase 12: Testing & Deployment

---

## [2.2.2] - 2026-01-02

### Public World Viewer Fixes

**Fixed:**
- Article tree auto-expand now correctly opens to selected article
- Virtual groups (Player Characters, Wiki) no longer contribute to URL paths
- Breadcrumbs now include virtual group names (Player Characters, Wiki, Campaign, Arc)
- Virtual group breadcrumbs are no longer clickable (disabled links)
- Article icons now render properly using IconDisplay component instead of showing raw Font Awesome class names
- Markdown rendering now uses Markdig via MarkdownService (replaces basic regex)
- Main content area now has white background as designed

**Changed:**
- PublicArticleTreeItem now properly tracks path accumulation for nested articles
- GetBreadcrumbItems detects virtual groups by Guid.Empty or known slugs
- BuildPublicBreadcrumbsAsync walks up article hierarchy to determine root article type

---

## [2.2.1] - 2026-01-01

### Infrastructure: Application Insights Integration

**Added:**
- Application Insights resource (`appi-chronicis-dev`) for telemetry and monitoring
- `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable in Azure and local settings
- Availability test configured to ping `/api/health` every 5 minutes (keeps Functions warm)

**Context:**
- Azure Functions cold starts were causing client timeouts on first API calls
- Availability test acts as a keep-alive ping to prevent scale-to-zero
- Telemetry provides logging, performance metrics, and error tracking for future debugging

---

## [2.2.0] - 2026-01-01

### Public World Sharing

**Added:**
- Public world sharing with globally unique slugs
- Three-tier article visibility system (Public, MembersOnly, Private)
- `World.IsPublic` and `World.PublicSlug` database columns
- `ArticleVisibility` enum with Public (0), MembersOnly (1), Private (2) values
- Public slug validation (3-100 chars, lowercase alphanumeric + hyphens)
- Reserved slug protection (api, admin, public, private, etc.)
- Auto-suggestion of alternative slugs when slug is taken
- PublicWorldService for anonymous world/article access
- PublicWorldFunctions API endpoints (AllowAnonymous)
- PublicApiService client without auth headers
- PublicWorldPage at `/w/{publicSlug}` and `/w/{publicSlug}/{*articlePath}`
- PublicArticleTreeItem component for sidebar navigation
- World settings "Public Sharing" section with toggle and slug editor
- Real-time slug availability checking with debounce
- Copy-to-clipboard button for public URLs
- Preview button to open public URL in new tab

**API Endpoints:**
- `POST /api/worlds/{id}/check-public-slug` - Check slug availability
- `GET /api/public/worlds/{publicSlug}` - Get public world (anonymous)
- `GET /api/public/worlds/{publicSlug}/articles` - Get public article tree (anonymous)
- `GET /api/public/worlds/{publicSlug}/articles/{*path}` - Get public article (anonymous)

**Changed:**
- WorldUpdateDto now includes IsPublic and PublicSlug properties
- WorldDto and WorldDetailDto now include IsPublic and PublicSlug
- Program.cs registers ChronicisPublicApi HttpClient without auth handler

---

## [2.1.0] - 2025-12-30

### Dashboard Redesign & Character Claiming

**Added:**
- Hero section with dark gradient background, welcome message, and inspirational quote
- World-centric dashboard with expandable world panels
- Character claiming system (claim/unclaim characters as yours)
- CharacterFunctions API (`/api/characters/claimed`, `/api/characters/{id}/claim`)
- DashboardFunctions API (`/api/dashboard`) for aggregated data
- Server-side prompt generation with priority-based rules
- PromptService evaluating user state for contextual suggestions
- DashboardApiService and CharacterApiService on client
- WorldPanel component showing campaigns, characters, and stats
- IconDisplay component for rendering Font Awesome icons and emojis
- Stats panel showing Worlds, Campaigns, Articles, Characters counts
- First-time user onboarding wizard (`/getting-started`)
- User onboarding flag (`HasCompletedOnboarding`)
- UserFunctions API (`/api/users/me`, `/api/users/me/complete-onboarding`)

**Changed:**
- Dashboard now prioritizes worlds with active campaigns
- Prompts displayed in hero section with color-coded categories
- World titles visually prominent, section headers subtle (overline style)
- Navigation uses forceLoad for reliable page transitions
- Character chips use custom div elements for proper click handling
- Welcome text and prompt titles use beige-gold color scheme

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
