# Chronicis - Changelog

All notable changes to this project are documented in this file.

## [Unreleased]

### Added — Cascade wiki-link title rewrite on article rename

When an article is renamed, all wiki-link spans in back-linked articles that accepted the
article's title as their default display text are automatically rewritten to the new title.
Links where the user set a custom display label are left untouched. The previous title is
recorded as an `ArticleAlias` on the renamed article so it remains discoverable via the
existing alias-resolution path.

**New services:**
- `IWikiLinkTitleRewriter` / `WikiLinkTitleRewriter` — stateless regex rewriter for a
  single article's HTML body; no DB access.
- `IArticleRenameCascadeService` / `ArticleRenameCascadeService` — write-orchestration
  service that queries back-linked sources, rewrites bodies, and appends the alias.

**Controller change:**
- `ArticlesController.UpdateArticle` captures `oldTitle` before mutation and invokes
  `CascadeTitleChangeAsync` after the save when the title changes (case-insensitive guard,
  so `"Foo" → "foo"` is treated as no-op).

**Edge cases documented and tested:** custom-display links, map chips, broken-link spans,
external-link spans, legacy `[[guid]]` syntax, zero-backlink renames, duplicate alias
dedup, case-only renames, HTML-encoding of special characters in new titles.

---

## [3.0.1] - 2026-04-19


### Fixed — Article renames never persisted (regression from Phase 5)

**Symptom:** Renaming an article showed a success toast, but reloading the page restored the old title. No errors on client or server.

**Root cause:** Commit `cb39986` ("Phase 5: ArticleDetailViewModel extracted") on 2026-02-23 moved the save logic out of `ArticleDetail.razor` into a new `ArticleDetailViewModel`. The razor's title input is bound to a razor-local field (`_editTitle`) via `@bind-Title`. The extracted VM has its own `EditTitle` property, initialized once in `LoadArticleAsync` and never updated by the razor. `SaveArticleAsync` originally took only the body as a parameter and used the stale `EditTitle` for the title, so every rename sent the originally-loaded title to the server. The server's 200 OK response reflected the unchanged record and the UI accepted it as success.

**Primary fix:**

- `ArticleDetailViewModel.SaveArticleAsync(string currentBody)` → `SaveArticleAsync(string currentBody, string currentTitle)`. The title now flows through the same explicit parameter-passing channel as the body, eliminating the reliance on stale VM state.
- `ArticleDetail.razor` passes its authoritative `_editTitle` on save.
- Regression test `SaveArticleAsync_ForwardsCurrentTitleFromRazorToApi` added: confirms the DTO sent to the API uses the caller-supplied title rather than VM-internal state.

**Additional resilience change — client:**

- `ArticleDetailViewModel.SaveArticleAsync` now checks the return value of `IArticleApiService.UpdateArticleAsync`. A null response (non-2xx from the server) surfaces as `SaveArticleResult.Failed` with an error notification, and local article state is not mutated. Previously the return value was discarded, which would have masked the rename bug and any future silent server failures. On success, the server's returned DTO is now the source of truth for local article fields. Regression test `SaveArticleAsync_WhenApiReturnsNull_ReturnsFailedAndDoesNotMutateLocalState` added.

**Latent bug fix — server (unrelated to the rename symptom):**

- `ReadAccessPolicyService.ApplyAuthenticatedReadableArticleFilter` rewritten from `IQueryable.Concat(...)` of two filtered queries into a single `Where(...)` predicate. EF Core translates set operators into SQL `UNION ALL`, and entities returned from set operations are materialized as **untracked**, regardless of the underlying `DbSet` tracking behavior. Any write path that retrieves an article through this filter, mutates it, and calls `SaveChangesAsync()` would silently no-op because the change tracker never sees the entity as `Modified`. `ArticlesController.UpdateArticle` is one such path. The rename symptom did not surface through this code path (the title itself was never changed before hitting the controller), but the architectural bug would have caused identical-looking silent failures in the future.

**Not changed:**

- No API contract changes. No database migrations. No client-facing behavior changes other than renames actually persisting.

---

## [3.0.0] - 2026-03-03

### Maps, Layers, and Basemap Image Workflow

**Added:**
- World-level Maps feature with dedicated routes:
  - `/world/{worldId}/maps` (Map Listing)
  - `/world/{worldId}/maps/{mapId}` (Map Detail)
- Maps virtual group in the world tree; clicking **Maps** opens Map Listing instead of creating an uncategorized wiki article.
- Map creation flow with required map name plus basemap image upload.
- Basemap upload supports file picker and drag/drop in the Map Listing create panel.
- Browser drag/drop guard to prevent dropped image files from opening in a new tab outside the drop zone.
- Map page header with inline map-name editing and explicit save controls (session-style save pattern).
- Map layer management on map pages and modal viewers:
  - Default World/Campaign/Arc layers plus custom layers.
  - Layer visibility toggles with immediate pin filtering.
  - Layer selection for pin placement, drag/drop reorder, and custom layer create/rename/delete controls.
- Polygon authoring workflow on map pages:
  - Click to add vertices directly on the basemap.
  - Live draft overlay while plotting the shape.
  - Save from the "Editing polygon" bar or double-click to close and save.
- Polygon editor states for both draft and selected polygons with:
  - Save / Cancel controls during draft creation.
  - Save / Cancel / Delete controls for existing polygons.
  - Draft progress and unsaved-change status messaging.
- Standardized polygon color palette:
  - `Blue`
  - `Green`
  - `Amber`
  - `Red`
  - `Teal`
- Polygon naming with editable text input and on-map label rendering near the polygon center.
- Vertex-editing affordances:
  - Direct drag handles on polygon vertices.
  - Click-on-edge insertion to add a new point to an existing polygon.
  - High-contrast circled-dot vertex markers with white backing for better visibility over map art.
- Breadcrumbs on both maps pages:
  - `Dashboard / {world name} / Maps`
  - `Dashboard / {world name} / Maps / {map name}`
- World-owner delete workflow with typed-name confirmation, matching Session Detail destructive-delete safety pattern.
- Session Note map linking:
  - Type `[[maps/` in Session Notes to autocomplete maps from the current world.
  - Selecting a map suggestion inserts an inline map chip.
  - Type `[[maps/{map}/` to autocomplete features on a specific resolved map.
  - Selecting a feature suggestion inserts an inline map-feature chip scoped to that map.
  - Clicking the chip opens the map in a modal viewer; feature chips center and highlight the selected feature.
- Public shared-world map viewing:
  - Public article rendering recognizes the same inline map and map-feature chips used in Session Notes.
  - Clicking a public map or map-feature chip opens the modal viewer in anonymous read-only mode.
  - Public feature chips target and highlight the selected feature without introducing anonymous persistence for modal layer toggles.

**Changed:**
- Map page basemap rendering is constrained to its content container to prevent horizontal overflow blowout.
- Tree behavior now expands/selects Maps and active map nodes when navigating maps routes.
- Map rename updates map node label in the tree immediately.
- Saved polygons now render as filled SVG regions using the selected standardized color instead of outline-only display.
- Polygon create mode now shows the editor bar immediately instead of waiting for polygon completion.
- Vertex hit-testing and drag behavior were hardened for zoomed-in editing so handles remain draggable at high zoom levels.
- Polygon selection and create-point coordinate resolution were hardened so editing still works after viewport/zoom changes.
- Map and map-feature chips now render as light gold-beige pills with a leading `📍` glyph and no leading type badge.

**Removed:**
- "Add Item" action from the Maps virtual group.

**API Endpoints:**
- `POST /world/{worldId}/maps`
- `GET /world/{worldId}/maps`
- `GET /world/{worldId}/maps/{mapId}`
- `PUT /world/{worldId}/maps/{mapId}`
- `DELETE /world/{worldId}/maps/{mapId}`
- `POST /world/{worldId}/maps/{mapId}/request-basemap-upload`
- `POST /world/{worldId}/maps/{mapId}/confirm-basemap-upload`
- `GET /world/{worldId}/maps/{mapId}/basemap`

**Storage and Data:**
- New maps tables for map metadata, layer defaults, and campaign/arc scoping pivots.
- Basemap binaries stored in blob storage under map-scoped folders.
- `MapFeatureCreateDto`, `MapFeatureUpdateDto`, and `MapFeatureDto` now round-trip polygon `Color` metadata alongside `Name`.
- `MapFeature` persistence now stores polygon color selection in addition to name and geometry references.
- Polygon geometry continues to use GeoJSON-shaped payloads with blob-backed storage for the compressed geometry body.
- Migration `20260313180329_AddMapFeatureColor` adds map-feature color persistence.
- Delete map permanently removes metadata records and the full map blob folder (no restoration path).

---

## [2.12.0] - 2026-02-26

### Collaborative Sessions, Tutorial World, Contextual Onboarding & GM Private Notes

**Multi-Author Session Notes**

- Session is a fully first-class domain entity under Arc
- On session creation a default `SessionNote` is automatically created for the creator
- Any world member can add their own `SessionNote` to a session via "Add Session Note"
- AI summary generation on Session Detail aggregates content from all `SessionNote` records for that session
- Players navigate to individual session notes via the Session Detail page note list

**Tutorial World**

- Sysadmin maintains a canonical tutorial world with rich pre-populated content
- On first login, the tutorial world is cloned for the new user so they have a real world to explore immediately
- Future improvements to the sysadmin world become the baseline for all subsequent new-user clones (existing users keep their current tutorial world)
- Clone operation creates a full copy of the world including articles, campaigns, arcs, and sessions
- `SysAdminTutorialsController` provides the endpoint for managing the canonical source world

**Contextual Onboarding Sidebar (`TutorialDrawer`)**

- New `TutorialDrawer` component renders as a right-side drawer alongside the metadata and quest drawers
- Content is resolved by `TutorialPageTypeResolver` based on the current URL and article type
- Supported page types: `world-detail`, `campaign-detail`, `arc-detail`, `session-detail`, `session-note`, `player-character`, `wiki`
- Tutorial content is authored by sysadmins via the Admin panel and stored as `Tutorial` entities
- When viewing the tutorial world, the drawer is pinned open (`IsForcedOpen = true`) and the close button is suppressed
- The existing `GettingStarted` wizard is retained; its final step now references the tutorial sidebar
- `ITutorialApiService` / `TutorialApiService` handle client-side content retrieval

**GM Private Notes (World, Campaign, Arc, Session)**

- `World`, `Campaign`, `Arc`, and `Session` entities each gain a `PrivateNotes` field (stored on the record)
- A "Private Notes" tab is added to World Detail, Campaign Detail, Arc Detail, and Session Detail pages
- The tab is only rendered for users with the GM role; non-GMs cannot see the tab or its content
- GMs can also create private `SessionNote` articles (existing privacy toggle) for more structured per-session planning
- No new migrations beyond the `PrivateNotes` column additions on the four entities

---

## 2026 Q1 Internal Architecture Consolidation

- Centralized article hierarchy logic
- Consolidated external link services
- Decomposed TreeStateService
- Split WorldService responsibilities
- Canonicalized EF migrations directory
- Extracted Program.cs configuration into extensions

No breaking API changes.

## [2.11.0] - 2026-02-13

### Inline Article Images

**Added:**
- Inline image upload in TipTap article editor via drag-and-drop, paste, or toolbar button
- Images uploaded to Azure Blob Storage and linked to articles via `WorldDocument.ArticleId`
- Stable `chronicis-image:{documentId}` reference format stored in article HTML
- Client-side SAS URL resolution on render via authenticated API call
- In-memory SAS URL cache to avoid redundant API calls within a session
- Image toolbar button above TipTap editor
- TipTap `@tiptap/extension-image@3.11.0` integration for proper `<img>` node rendering
- `ImagesController` proxy endpoint (`GET /api/images/{documentId}`) for authenticated image access
- Automatic image cleanup when articles are deleted (blobs + DB records)
- Inline images filtered from treeview's External Resources section (still visible in campaign document list)

**API Endpoints:**
- `GET /api/images/{documentId}` - Authenticated image proxy (302 redirect to SAS URL)

**Schema Changes:**
- `WorldDocument.ArticleId` nullable FK added (links inline images to their article)
- Filtered index `IX_WorldDocuments_ArticleId` for efficient article image queries
- `DeleteBehavior.SetNull` on FK (article deletion nullifies, cleanup handled explicitly)

**Technical:**
- `imageUpload.js` handles paste, drop, and file picker with file type/size validation (PNG, JPEG, GIF, WebP; 10 MB max)
- Upload flow: validate → upload to blob via SAS URL → confirm → insert `chronicis-image:{documentId}` → resolve to SAS URL
- `resolveEditorImages()` called on editor init to resolve stored references to fresh SAS URLs
- `IWorldDocumentService.DeleteArticleImagesAsync()` for bulk image cleanup during article deletion
- `TreeDataBuilder` filters documents with `ArticleId != null` from External Resources group

**Migration:**
- `20260211233703_AddArticleIdToWorldDocument`

---

## [2.10.0] - 2026-02-08

### Quest Tracking System

**Added:**
- Complete quest tracking system integrated into session workflow
- Quest entities with status lifecycle (Active, Completed, Failed)
- Quest update journal with markdown editor and wiki links
- Quest drawer accessible via Ctrl+Q keyboard shortcut from session pages
- Real-time quest status management (activate, complete, fail)
- Quest-to-session associations for tracking progress across gameplay
- Quest update entries with timestamps and markdown content
- Recent updates timeline showing last 10 quest journal entries
- TipTap markdown editor integration in quest drawer
- Wiki link autocomplete in quest updates (`[[article]]` and `[[srd/resource]]`)
- External resource linking in quest descriptions (D&D SRD integration)

**Components:**
- QuestDrawer - Slide-out panel for quest management during sessions
- QuestSelector - Dropdown for choosing which quest to update
- QuestEditor - Inline editing for quest title and description
- QuestUpdateEntry - Journal entry display with formatted markdown
- WikiLinkAutocomplete - Shared autocomplete component for all editors

**API Endpoints:**
- `GET /api/arcs/{arcId}/quests` - List quests for an arc
- `POST /api/arcs/{arcId}/quests` - Create new quest
- `GET /api/quests/{questId}` - Get quest details
- `PUT /api/quests/{questId}` - Update quest metadata
- `DELETE /api/quests/{questId}` - Delete quest
- `PUT /api/quests/{questId}/status` - Change quest status
- `POST /api/quests/{questId}/updates` - Add quest update entry
- `GET /api/quests/{questId}/updates/recent` - Get recent updates for timeline

**Features:**
- Context-aware quest drawer (only available from session/session note pages)
- Auto-association toggle for linking updates to current session
- Quest status badges with color coding (green=active, blue=completed, red=failed)
- Empty state guidance when no quests exist for arc
- Validation preventing quest creation without title
- Keyboard shortcuts for improved workflow efficiency

**Architecture:**
- Event-driven service layer (WikiLinkAutocompleteService)
- Shared component pattern for wiki link autocomplete
- Scoped services for state management
- TipTap editor with custom Chronicis extensions
- JavaScript interop for advanced editor features

**Technical:**
- Quest entity with ArcId, SessionId (optional), Status, Title, Description
- QuestUpdate entity with QuestId, SessionId (optional), Body, CreatedAt
- Database migrations: `20260207223643_FixQuestModelChanges`
- Z-index optimization for drawer overlay components (9999 for autocomplete)
- WorldId context resolution from session article hierarchy
- Event-driven UI updates via OnShow, OnHide, OnSuggestionsUpdated events

**UI/UX:**
- Consistent styling with Chronicis design language (beige-gold accents)
- Responsive drawer with metadata panels and editor sections
- Loading states and empty states throughout
- Toast notifications for successful operations
- Validation feedback for user actions

**Documentation:**
- QUEST_AUTOCOMPLETE_FINAL_SUMMARY.md - Implementation details
- WIKILINK_AUTOCOMPLETE_IMPLEMENTATION_COMPLETE.md - Architecture guide

---

## [2.9.0] - 2026-01-13

### External Links: Open5e API Integration

**Added:**
- Full Open5e API integration with 10 SRD categories:
  - Spells, Monsters, Magic Items, Conditions, Backgrounds
  - Feats, Classes, Races, Weapons, Armor
- Category selection UI with icons when typing `[[srd/`
- Search within categories (e.g., `[[srd/spells/fire`)
- Styled preview drawer matching metadata sidebar design
- Category-specific markdown formatting for each content type

**Changed:**
- Migrated from Open5e v1 API to v2 API exclusively
- External link preview drawer now uses soft off-white background (#F4F0EA)
- Headers in preview content now use dark blue-grey color
- Close button repositioned to top-right corner of preview drawer

**Technical:**
- Document filtering uses `document__gamesystem__key=a5e` for SRD content
- Name-based search with `name__contains` parameter and client-side filtering
- Simplified `CategoryConfig` record (removed unused `ApiVersion` field)
- v2 API endpoints: creatures (monsters), items (magic items), etc.
- Object property extraction for v2 API fields (size, type returned as objects)

**Infrastructure:**
- Added `RemoveAllLoggers()` to HttpClient registrations to reduce log noise
- Suppressed EF Core SQL command logging in development

---

## [2.8.0] - 2026-01-11

### Infrastructure: API Migration to Azure App Service

**Changed:**
- Migrated API from Azure Static Web Apps managed functions to dedicated Azure App Service
- API now hosted at `api.chronicis.app` (App Service: `app-chronicis-api-dev`)
- Converted from Azure Functions Isolated Worker to standard ASP.NET Core Web API
- Authentication now uses standard ASP.NET Core JWT Bearer middleware instead of custom middleware
- Removed `X-Auth0-Token` header workaround (Azure SWA was intercepting standard `Authorization` header)
- Client now uses standard `Authorization: Bearer` header for all API requests
- Separate GitHub Actions workflow for API deployment

**Technical:**
- `LinksController` removed; endpoints moved to `ArticlesController` and `WorldsController`
- API routes now follow RESTful conventions:
  - `articles/{id}/backlinks` (was `links/backlinks/{id}`)
  - `articles/{id}/outgoing-links` (was `links/outgoing/{id}`)
  - `articles/resolve-links` (was `links/resolve`)
  - `articles/{id}/auto-link` (was `links/auto-link/{id}`)
  - `worlds/{id}/link-suggestions` (was `links/suggestions?worldId={id}`)
- Response DTOs properly wrapped (`BacklinksResponseDto`, `LinkSuggestionsResponseDto`)

**Benefits:**
- Standard ASP.NET Core authentication patterns
- No more token header workarounds
- Better debugging and logging capabilities
- Easier local development without Azure Functions runtime
- More predictable cold start behavior

---

## [2.7.0] - 2026-01-09

### Added
- External knowledge links in the article editor using wiki-style autocomplete
  - Trigger external sources with `[[sourceKey/` (initial provider: `srd`)
  - External links are stored as stable tokens: `[[source|id|title]]`
- In-app preview drawer for external links
  - Preview content is fetched live from the provider API
  - Content is rendered as normalized Markdown
  - Optional link to open the source site in a new tab
- Provider-based external link architecture
  - External sources are keyed by prefix (example: `srd`)
  - Architecture supports adding additional providers without editor changes

### Improved
- Editor linking experience now supports mixing internal articles and external references seamlessly
- Autocomplete behavior now routes intelligently based on link prefix

### Technical
- New API endpoints for external link integration:
  - `GET /api/external-links/suggestions`
  - `GET /api/external-links/content`
- Added server-side provider abstraction for external data sources
- Added SSRF-safe validation for external content identifiers
- External link preview content cached in-memory per session to reduce repeat API calls


---

## [2.6.0] - 2026-01-03

### Export & Settings

**Added:**
- Export world data to Markdown zip archive
- Settings page at `/settings` with Profile, Data, and Preferences tabs
- World selector for choosing which world to export
- YAML frontmatter in exported files (title, type, visibility, dates, icon)
- AI Summary section included in exported markdown files
- Folder structure matching tree hierarchy (`ArticleName/ArticleName.md` pattern)
- Nested list support in HTML→Markdown conversion for export
- Profile section showing user info from Auth0
- Data management section with export UI
- Preferences section with placeholders for future settings
- Settings link in user dropdown menu
- `chronicisDownloadFile` JavaScript function for triggering browser downloads

**Export Folder Structure:**
```
WorldName/
├── Wiki/
│   └── Locations/
│       └── Waterdeep/
│           ├── Waterdeep.md
│           └── Castle Ward/
│               └── Castle Ward.md
├── Characters/
│   └── Character Name/
│       └── Character Name.md
└── Campaigns/
    └── Campaign Name/
        ├── Campaign Name.md
        └── Arc Name/
            ├── Arc Name.md
            └── Session 1/
                └── Session 1.md
```

**API Endpoints:**
- `GET /api/worlds/{worldId}/export` - Generate and download world export zip

**Technical:**
- ExportService with recursive folder building
- HtmlToMarkdown converter handles nested lists, wiki links, formatting
- Server-side zip generation with streaming response
- MudTheme now properly wired to MudThemeProvider in layouts

**Fixed:**
- TipTap editor now stores HTML directly (fixes nested list formatting loss on reload)
- Custom Chronicis theme colors now apply to MudBlazor components

---

## [2.5.0] - 2026-01-03

### Document Storage

**Added:**
- Document upload and management for worlds (PDF, DOCX, XLSX, PPTX, TXT, MD, images)
- Azure Blob Storage integration with SAS token security
- WorldDocument entity with metadata tracking
- BlobStorageService for file operations
- Direct client-to-blob upload (bypasses 4MB HTTP limit)
- WorldDocumentUploadDialog with file validation
- Documents section on WorldDetail page with inline editing
- Document list/management panel with title, description, size, upload date
- File-type-specific icons in tree navigation (PDF, Word, Excel, PowerPoint, image, text)
- Auto-rename for duplicate filenames (e.g., "Document (2).pdf")
- 200 MB file size limit with client-side validation
- 15-minute SAS token expiration for security
- Download via temporary SAS URLs

**API Endpoints:**
- `POST /api/worlds/{id}/documents/request-upload` - Request upload SAS URL
- `POST /api/worlds/{id}/documents/{documentId}/confirm` - Confirm upload completion
- `GET /api/worlds/{id}/documents` - List world documents
- `GET /api/documents/{documentId}/content` - Stream document content
- `PUT /api/worlds/{id}/documents/{documentId}` - Update document metadata
- `DELETE /api/worlds/{id}/documents/{documentId}` - Delete document

**Changed:**
- TreeStateService loads documents alongside links in External Resources section
- TreeNode.AdditionalData dictionary stores document metadata
- ArticleTreeNode handles document download via click
- InputFile component used instead of MudFileUpload (fixes Blazor WASM file reference bug)
- Files read into byte array immediately upon selection to avoid disposal issues

**Permissions:**
- Upload/Edit/Delete: World owner (GM) only
- Download/View: All world members

**Infrastructure:**
- Azure Blob Storage container: `chronicis-documents` (private access)
- CORS configuration required for browser uploads
- Blob path pattern: `worlds/{worldId}/documents/{documentId}/{filename}`
- NuGet package: Azure.Storage.Blobs v12.22.2

**Documentation:**
- Document-Storage-Testing-Guide.md (33 test scenarios)
- Document-Storage-User-Guide.md (user-facing documentation)
- Document-Storage-API.md (API reference with examples)
- Document-Storage-Implementation-Summary.md (technical overview)

---

## [2.4.0] - 2026-01-02

### Private Articles

**Added:**
- Privacy toggle in article metadata drawer (right panel)
- Only article creators can toggle their articles' privacy
- Private articles show lock icon in tree view (replaces normal icon)
- Lock icon updates immediately when toggling privacy (no tree reload)
- `UpdateNodeVisibility()` method in TreeStateService for real-time UI updates

**Backend:**
- `GetAccessibleArticles()` helper filters private articles server-side
- Private articles only returned to their creator via WorldMembers join
- Refactored all ArticleService methods to use centralized access helper

**UI:**
- Private articles display 🔒 lock icon instead of article type icon
- Tooltip "Private - only you can see this" on hover
- `.tree-node__icon--private` CSS class with muted gold styling

---

## [2.3.1] - 2026-01-02

### World Membership Article Access Fix

**Fixed:**
- Invited users can now see and navigate articles in worlds they've joined
- Article tree loads correctly for non-owner world members
- Clicking articles in nav tree now works for all world members

**Changed:**
- Refactored ArticleService to use `GetAccessibleArticles()` IQueryable helper
- Replaced `CreatedBy == userId` checks with WorldMembers join pattern
- All article queries now check world membership instead of article ownership
- AutoLinkService updated to use WorldMembers for linkable articles

**Technical:**
- Added reusable `GetAccessibleArticles(Guid userId)` method
- Uses LINQ join: `Articles JOIN WorldMembers ON WorldId WHERE UserId`
- Private articles filtered: `Visibility != Private OR CreatedBy == userId`

---

## [2.3.0] - 2026-01-02

### Multi-User World Collaboration

**Added:**
- World-level membership system (replaces campaign-level membership)
- `WorldMember` entity with Role (GM, Player, Observer)
- `WorldInvitation` entity with memorable codes (XXXX-XXXX format)
- `WorldRole` enum for role-based access control
- Invitation code generator with pronounceable patterns
- WorldMembersPanel component for member/invitation management
- JoinWorldDialog for entering invitation codes
- "Join a World" button on Dashboard hero section
- Member list with role display and management
- Role change dropdown (GM only)
- Remove member functionality (GM only)
- Create invitation with auto-copy to clipboard
- Revoke invitation functionality
- Member count in World Statistics

**API Endpoints:**
- `GET /api/worlds/{id}/members` - List world members
- `PUT /api/worlds/{worldId}/members/{memberId}` - Update member role
- `DELETE /api/worlds/{worldId}/members/{memberId}` - Remove member
- `GET /api/worlds/{id}/invitations` - List invitations (GM only)
- `POST /api/worlds/{id}/invitations` - Create invitation (GM only)
- `DELETE /api/worlds/{worldId}/invitations/{invitationId}` - Revoke invitation
- `POST /api/worlds/join` - Join world via invitation code

**Changed:**
- New users no longer get an auto-created default world
- Dashboard now shows "Create New World" and "Join a World" buttons
- World detail page includes Members & Invitations section
- Membership is now at world level (all campaigns in a world share members)

**Removed:**
- `CampaignMember` entity (replaced by WorldMember)
- `CampaignRole` enum (replaced by WorldRole)
- Auto-creation of default world for new users

**Database:**
- Migration `20260102041237_WorldMembership` converts CampaignMembers to WorldMembers

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

