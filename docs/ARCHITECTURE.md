# Chronicis Architecture Inventory

Last reviewed: 2026-04-27

## 1) Scope
- Projects covered:
- `src/Chronicis.Api`
- `src/Chronicis.Client`
- `src/Chronicis.Client.Engine`
- `src/Chronicis.Shared`
- Purpose:
- Capture the implemented architecture (composition roots, boundaries, dependencies, persistence model, integration adapters, and conventions).
- Exclusions:
- `src/Chronicis.CaptureApp` (explicitly out of scope for this inventory).

## 2) System Topology

### 2.1 Runtime Components
- Browser-hosted Blazor WebAssembly app (`Chronicis.Client`) serves UI and user interaction workflows.
- Client-side geometry/static-asset package (`Chronicis.Client.Engine`) supplies polygon-editing algorithms and ships the map-engine static web asset consumed by client builds.
- ASP.NET Core API (`Chronicis.Api`) provides authenticated and public endpoints.
- SQL Server (via EF Core) stores application state and relational graph.
- Azure Blob Storage stores world documents, inline article images, and map basemap files.
- Azure OpenAI provides summary generation.
- External content providers (Open5e + blob-backed SRD/ROS datasets) provide third-party reference content.

### 2.2 Dependency Direction (Compile-Time)
```text
Chronicis.Client  ---->  Chronicis.Client.Engine  ---->  Chronicis.Shared
Chronicis.Client  ---->  Chronicis.Shared
Chronicis.Api     ---->  Chronicis.Shared
Chronicis.Client.Engine  ---->  Chronicis.Shared
Chronicis.Shared         ---->  (no project references)
```

### 2.3 Request/Response Topology
- Authenticated flows:
- Browser -> Client API service layer -> API controllers -> domain services -> EF Core / external adapters.
- Public flows:
- Browser -> Public pages + public API client -> anonymous API endpoints (public world/document health surfaces).

## 3) Chronicis.Api Architecture

### 3.1 Composition Root and Pipeline
- `Program.cs` is the API composition root.
- Registers MVC controllers and endpoint routing via `MapControllers()`.
- Configures Auth0 JWT bearer auth with audience/issuer validation.
- Enables authentication + authorization middleware globally.
- Configures CORS for production and local origins.
- Configures EF Core SQL Server with retry policy and command timeout.
- Registers all domain services via DI as scoped dependencies.
- Registers external provider system (Open5e and blob-backed providers).
- Registers system health check services.
- Configures Serilog and Datadog diagnostics at startup.

### 3.2 Layering Model
- Controllers layer:
- Thin HTTP boundary (`Controllers/*`) handling request validation, status mapping, and orchestration.
- Application/domain services layer:
- Core behaviors in `Services/*` behind interfaces (e.g., `IWorldService`, `ISessionService`, `ISummaryService`).
- Infrastructure layer:
- Runtime identity resolution and cross-cutting runtime integration in `Infrastructure/*`.
- Persistence layer:
- EF Core data context in `Data/ChronicisDbContext.cs`.
- Targeted repository abstraction in `Repositories/*` (resource-provider persistence logic).
- Shared contract/domain layer:
- Entity, DTO, enum, and utility contracts imported from `Chronicis.Shared`.

### 3.3 Service Domain Modules
- Content graph module:
- `ArticleService`, `ArticleValidationService`, `ArticleHierarchyService`, `LinkParser`, `LinkSyncService`, `AutoLinkService`, `ArticleExternalLinkService`.
- World/collaboration module:
- `WorldService`, `WorldMembershipService`, `WorldInvitationService`, `WorldPublicSharingService`, `ResourceProviderService`.
- Campaign progression module:
- `CampaignService`, `ArcService`, `SessionService`, `QuestService`, `QuestUpdateService`.
- AI module:
- `SummaryService` with article/campaign/arc/session summary pipelines.
- File/export module:
- `BlobStorageService`, `WorldDocumentService`, `ExportService` (+ markdown builder partials).
- Maps module:
- `WorldMapService` + `IMapBlobStore`/`AzureBlobMapBlobStore` handle map metadata, basemap SAS flows, point/polygon feature CRUD, polygon geometry blob persistence, session-note nested map/feature autocomplete reads, and destructive map-folder cleanup.
- Routing module:
- `ISlugPathResolver` / `SlugPathResolver` traverses entity slug chains (world → campaign → arc → session → article/map) and returns a typed `SlugPathResolution`.
- `IReservedSlugProvider` / `ReservedSlugProvider` enforces the `Routing:ReservedSlugs` allowlist.
- `RoutingOptions` carries configuration-driven reserved-slug and routing policy settings.
- Unified path endpoint (`PathsController`, `GET /api/paths/resolve/{*path}`) is anonymous-accessible and returns the resolved entity kind and IDs.
- Public read model module:
- `PublicWorldService` for anonymous world/article/document access projections plus anonymous map/modal read models for shared worlds.
- Admin/tutorial module:
- `AdminService`, `TutorialService`.
- Prompting module:
- `PromptService` for dashboard contextual prompts.
- Health module:
- `SystemHealthService` + provider-specific health services.

### 3.3.1 Consolidation Decisions
- Hierarchy traversal and breadcrumb/path construction are centralized in `IArticleHierarchyService`/`ArticleHierarchyService` instead of being reimplemented across controllers/services.
- The centralized hierarchy service includes guardrails for cyclic graphs and abnormal depth, and supports both authenticated and public breadcrumb modes.
- World concerns are intentionally split so `WorldService` owns core world CRUD while membership, invitation, and public-sharing policies are handled by dedicated services.

### 3.4 AuthN/AuthZ Architecture
- JWT bearer authentication is enforced by default on API controllers.
- User identity resolution is centralized in `CurrentUserService` using HTTP claims.
- Request-scope user caching avoids duplicate per-request DB lookups.
- First-seen users are provisioned/updated through `IUserService`.
- Sysadmin authorization is policy-by-checker (`ISysAdminChecker`) using configured Auth0 IDs/emails.
- Domain authorization is mostly service-level rule enforcement (owner checks, membership checks, role checks).

### 3.5 External Integration Architecture
- Azure OpenAI adapter:
- `SummaryService` composes prompts, estimates tokens/cost, executes chat completion, persists outputs.
- Blob storage adapter:
- `BlobStorageService` encapsulates pathing, SAS generation (upload/download), and blob CRUD.
- Document service composes DB record lifecycle + blob lifecycle around this adapter.
- External reference providers:
- `IExternalLinkProvider` abstraction with provider registry.
- `Open5eExternalLinkProvider` (live HTTP API strategy-based category handlers).
- `BlobExternalLinkProvider` (hierarchical blob discovery, cached indexes, progressive drill-down search).
- Provider orchestration/caching:
- `ExternalLinkService` centralizes source validation, world-provider gating, content/suggestion cache keys, exception normalization.
- External-link cache policy:
- Suggestion responses and content responses use separate short-lived cache windows to balance responsiveness and freshness.
- Current defaults in service code are 2-minute suggestion caching and 5-minute content caching.

### 3.6 Persistence Architecture (EF Core)
- Single DbContext:
- `ChronicisDbContext` defines all core sets and fluent model configuration.
- Relationship architecture:
- World is the top isolation boundary.
- Campaigns belong to worlds; arcs belong to campaigns; sessions belong to arcs.
- Articles are hierarchical (self-parenting) with optional world/campaign/arc/session scope.
- World collaboration entities (`WorldMember`, `WorldInvitation`) are world-scoped.
- Link graph entities (`ArticleLink`, `ArticleExternalLink`, `ArticleAlias`) attach to articles.
- Document entities (`WorldDocument`) are world-scoped with optional article association.
- Map entities (`WorldMap`, `MapLayer`, `MapFeature`, `WorldMapCampaign`, `WorldMapArc`) are world-scoped with optional campaign/arc scoping pivots.
- Quest entities are arc-scoped with quest-update timeline entities and optional session reference.
- Slug columns per entity (added in `UrlRestructure_SlugFoundations`):
- `World.Slug`: globally unique, replaces the former `PublicSlug` field.
- `Campaign.Slug`: sibling-unique per world.
- `Arc.Slug`: sibling-unique per campaign.
- `Session.Slug`: sibling-unique per arc.
- `WorldMap.Slug`: sibling-unique per world.
- Articles retain sibling-unique slug under the article tree (unchanged).
- Constraint architecture:
- Unique indexes for identity/business constraints (e.g., owner+slug, public slug, invitation code, sibling slug uniqueness).
- Filtered indexes for nullable/conditional uniqueness.
- Optimistic concurrency for quests via SQL rowversion.
- Delete behavior architecture:
- Mix of cascade/restrict/set-null/no-action selected per relationship to avoid SQL multi-cascade path issues and preserve required history.
- Migration source of truth:
- `src/Chronicis.Api/Migrations` is the canonical migration location.
- `ChronicisDbContextModelSnapshot.cs` under that folder is the authoritative model snapshot used by EF migrations.

### 3.7 API Surface Organization
- Controller modules map to explicit bounded areas:
- Worlds, world links, world documents, campaigns, arcs, sessions, articles, search, external links, quests/updates, users/dashboard, public sharing, admin/tutorials, summaries, resource providers, health, paths.
- `PathsController` exposes `GET /api/paths/resolve/{*path}` anonymously; all other entity controllers remain auth-required unless explicitly annotated.
- Public and authenticated endpoints are split by controller intent and authorization attributes.

### 3.8 Cross-Cutting Patterns
- Logging sanitization uses shared extension methods to scrub user-provided values.
- AsNoTracking is used widely for read paths.
- Parallelization is used in selected aggregate builders (dashboard, health, tree-oriented data assembly).
- Service interfaces are first-class and broadly used for testability and DI boundaries.

## 4) Chronicis.Client Architecture

### 4.1 Composition Root
- `Program.cs` is intentionally minimal and delegates registration to extension modules:
- `AuthenticationServiceExtensions`
- `MudBlazorServiceExtensions`
- `HttpClientServiceExtensions`
- `ApplicationServiceExtensions`
- `ServiceCollectionExtensions`
- Sets up:
- OIDC authentication
- MudBlazor theme/providers
- Local storage
- Named HTTP clients
- API/domain/state/viewmodel services
- Composition-root evolution:
- The client startup file is intentionally slim (about 30 lines) and defers registration concerns into extension modules for repeatability and lower change blast radius.

### 4.2 Client Layering Model
- Routing/layout layer:
- `App.razor` hosts Blazor routing. `PathResolver` (`/{*Path}`) is the single catch-all page for all entity-detail URLs; it resolves slug paths via `IPathApiService` and selects layout based on auth state.
- Non-entity pages (dashboard, settings, change-log, etc.) retain their own `@page` directives and layouts unchanged.
- Page layer:
- Route-bound pages in `Pages/*` are top-level screens.
- Component layer:
- Reusable UI and workflow components in `Components/*`.
- ViewModel layer:
- `ViewModels/*` use `INotifyPropertyChanged` (`ViewModelBase`) for page state and orchestration.
- Service layer:
- API clients, app state, drawer/shortcut coordination, markdown/render helpers.
- Shared map-geometry layer:
- `Chronicis.Client.Engine` centralizes polygon draft state, hit testing, SVG path building, GeoJSON conversion, and vertex editing so map workflows do not duplicate geometry logic inside page code.
- Infrastructure abstraction layer:
- `Infrastructure/*` adapters isolate navigation/dialog/title/notification concerns from viewmodels.
- JS interop layer:
- `wwwroot/js/*` modules provide editor/shortcut/upload/autocomplete interop.

### 4.3 Routing and Layout Architecture
- `PathResolver` (`@page "/{*Path}"`) is the single entry point for all entity-detail renders:
- calls `IPathApiService.ResolveAsync(path)` to obtain a `SlugPathResolution` containing the entity kind and IDs.
- selects layout via `SelectLayout(isAuthenticated)`: `AuthenticatedLayout` for signed-in users, `PublicLayout` for anonymous visitors.
- dispatches to the appropriate detail component (`WorldDetail`, `CampaignDetail`, `ArcDetail`, `SessionDetail`, `ArticleDetail`, `MapListing`, `MapDetail`) passing entity IDs as component parameters.
- reserved slugs (configured under `Routing:ReservedSlugs` — e.g., `dashboard`, `settings`) redirect to `/dashboard` before the API call.
- `AuthenticatedLayout` hosts:
- top app bar
- left navigation drawer/tree
- global search
- user/admin menus
- global drawer host
- keyboard bridge
- `PublicLayout` hosts public nav/footer and renders anonymous/public pages.

### 4.4 HTTP and API Client Architecture
- Named HTTP clients:
- `ChronicisApi` (authenticated)
- `ChronicisPublicApi` (anonymous/public)
- Auth handler:
- `ChronicisAuthHandler` injects bearer token via `IAccessTokenProvider`.
- API service pattern:
- Interface + implementation per domain endpoint cluster (e.g., `IArticleApiService` / `ArticleApiService`).
- Path resolution client seam:
- `IPathApiService` / `PathApiService` calls `GET /api/paths/resolve/{*path}` (anonymous-accessible) and returns a `SlugPathResolution`; consumed by `PathResolver` to dispatch to the correct detail component.
- Map-linking client seams:
- `IMapApiService` / `MapApiService` serve authenticated map CRUD and nested `[[maps/...` autocomplete reads for editor map/map-feature chips.
- `IPublicApiService` / `PublicApiService` serve anonymous public-world content reads and modal map hydration for public chip clicks.
- Shared HTTP utility extension methods unify GET/POST/PUT/PATCH/DELETE error handling and logging behavior.

### 4.5 State and Coordination Architecture
- App context state:
- `AppContextService` tracks selected world/campaign and persists selection to local storage.
- Tree state subsystem:
- `TreeStateService` is a facade over decomposed internals:
- `TreeDataBuilder` (fetch/build graph)
- `TreeNodeIndex` (indexed graph storage)
- `TreeUiState` (selection/expansion/search/persistence)
- `TreeMutations` (create/move/delete/update + refresh callback orchestration)
- Tree map integration:
- `TreeDataBuilder` hydrates a virtual `Maps` group plus map nodes per world using `IMapApiService`.
- Tree selection/expansion supports map routes and map-node display updates after rename.
- Drawer coordination:
- `DrawerCoordinator` enforces mutually-exclusive right-side drawers with forced-open support.
- Quest/metadata/tutorial drawers wrap coordinator APIs with focused behaviors/events.
- Keyboard coordination:
- `KeyboardShortcutService` and JS bridge route global key events into domain actions.
- Public tree-state contract stability:
- `ITreeStateService` remains the stable public facade while tree responsibilities are decomposed internally (`TreeDataBuilder`, `TreeNodeIndex`, `TreeUiState`, `TreeMutations`).
- This keeps callers stable while allowing internal maintenance/refactoring without page-level contract churn.

### 4.6 ViewModel Architecture
- Viewmodels are transient, page-oriented orchestration units.
- They consume service interfaces rather than component primitives.
- UI infrastructure abstractions (`IAppNavigator`, `IUserNotifier`, `IConfirmationService`, `IPageTitleService`) decouple business logic from UI framework details.
- Property change notifications are explicit and standardized via `ViewModelBase`.

### 4.7 Rendering and Content Architecture
- MudBlazor is the primary component framework.
- Markdown rendering pipeline is encapsulated in `MarkdownService`.
- Render-definition subsystem (`RenderDefinitionService` + `wwwroot/render-definitions`) supports structured external content rendering.
- Version metadata is read from CI-stamped `wwwroot/version.json` via `VersionService`.

### 4.8 JS Interop Boundaries
- Editor and content interop modules:
- `tipTapIntegration.js`
- `wikiLinkAutocomplete.js`
- `wikiLinkExtension.js`
- `imageUpload.js`
- `mapsDropGuard.js`
- Navigation/interaction interop modules:
- `keyboardShortcuts.js`
- `publicWikiLinks.js`
- `questEditor.js`
- Auxiliary UX/diagnostic modules:
- `emojiPickerInterop.js`
- `chronicis-map-engine.js` (static web asset packaged via `Chronicis.Client.Engine`)
- `rum.js`
- Map chip interop responsibilities:
- `wikiLinkAutocomplete.js` routes nested `[[maps/...` editor autocomplete between world-map suggestions and feature suggestions scoped to a resolved map.
- `wikiLinkExtension.js` renders inline map/map-feature chips as `📍` pills and forwards chip click metadata into Blazor event handlers.
- `publicWikiLinks.js` binds rendered public map/map-feature chips to anonymous modal-open callbacks so public pages reuse the shared map viewer in read-only mode.

### 4.9 Inline Image Sub-Architecture
- Inline image persistence model:
- Images are persisted as `WorldDocument` records with optional `ArticleId` association.
- Stable editor reference model:
- Rich-text content stores `chronicis-image:{documentId}` tokens rather than directly storing expiring blob URLs.
- Runtime resolution model:
- Editor initialization resolves those stable tokens to fresh SAS-backed URLs (`resolveEditorImages`) and caches resolved URLs in-session to reduce repeat lookups.
- Upload orchestration model:
- Client JS validates and uploads bytes directly to blob SAS URLs after requesting upload intent from API, then confirms upload and inserts the stable tokenized image source.
- Cleanup model:
- Recursive article delete flows invoke `IWorldDocumentService.DeleteArticleImagesAsync(...)` so blob artifacts and DB metadata are removed together.

## 5) Chronicis.Shared Architecture

### 5.1 Role in the Solution
- `Chronicis.Shared` is the shared contract and domain kernel used by both Client and API.
- Contains no project-to-project dependencies.

### 5.2 Domain Model Layer
- Entity set in `Models/*` defines core relational domain primitives:
- User/world/collaboration: `User`, `World`, `WorldMember`, `WorldInvitation`
- Narrative hierarchy: `Campaign`, `Arc`, `Session`, `Article`
- Linking/resource entities: `ArticleLink`, `ArticleAlias`, `ArticleExternalLink`, `WorldLink`, `WorldDocument`
- Map entities: `WorldMap`, `MapLayer`, `MapFeature`, `WorldMapCampaign`, `WorldMapArc`
- Questing: `Quest`, `QuestUpdate`
- Configuration/content templates: `SummaryTemplate`, `TutorialPage`, `ResourceProvider`, `WorldResourceProvider`

### 5.3 Contract/DTO Layer
- DTO modules in `DTOs/*` provide API contract boundaries by domain area:
- articles, worlds, campaigns/arcs/sessions, maps, links/external links, summaries, search, dashboard, admin/tutorials, health, users, characters, resource providers.
- DTO shapes support both hierarchical and flat read models (e.g., `ArticleDto` + `ArticleTreeDto`).

### 5.4 Enums and Policy Encoding
- `Enums/*` encode business policy dimensions:
- `ArticleType`
- `ArticleVisibility`
- `WorldRole`
- `QuestStatus`

### 5.5 Shared Utilities and Cross-Cutting
- Admin utility module:
- `ISysAdminChecker`, `SysAdminChecker`, `SysAdminOptions`.
- Logging sanitization extension:
- `LoggerExtensions` routes log arguments through `LogSanitizer`.
- Utility helpers:
- `SlugGenerator` centralizes slug normalization/validation/uniqueness generation.

## 6) Key Data Relationship Inventory

### 6.1 Ownership and Membership
- World has a single owner (`User`) and many members (`WorldMember`).
- Membership is unique per world/user pair.
- Invitations are world-scoped and globally unique by invitation code.

### 6.2 Content Graph
- Article tree is self-referential through `ParentId`.
- Article scoping supports world/campaign/arc/session dimensions.
- Session notes attach to first-class sessions via `Article.SessionId`.

### 6.3 Link Graph
- Internal link edges are normalized in `ArticleLink` (source -> target).
- External references are normalized in `ArticleExternalLink` keyed by source + external ID.
- Alias terms are normalized in `ArticleAlias` with per-article uniqueness.

### 6.4 Artifact Storage
- `WorldDocument` records map metadata/state for blob-backed files.
- Optional `ArticleId` supports inline content image association.
- `WorldMap` stores basemap metadata (`BasemapBlobKey`, content type, original filename) for blob-backed map imagery; each map carries a `Slug` that is sibling-unique per world and is used in URL path generation.
- `MapFeature` stores point coordinates inline and stores polygon geometry references (`GeometryBlobKey`, `GeometryETag`) plus feature-level name/color metadata for blob-backed polygon shapes.

### 6.6 Slug Identity Inventory
- `World.Slug`: globally unique across all worlds; used as the first segment of every entity URL.
- `Campaign.Slug`: unique among campaigns within the same world.
- `Arc.Slug`: unique among arcs within the same campaign.
- `Session.Slug`: unique among sessions within the same arc.
- `WorldMap.Slug`: unique among maps within the same world.
- `Article.Slug`: unique among articles sharing the same parent (sibling-unique in the article tree).

### 6.5 Progress Tracking
- Quests are arc-scoped.
- Quest updates are quest-scoped timeline records with optional session association.
- Quest rowversion enables optimistic concurrency semantics.

## 7) Configuration Architecture

### 7.1 API Configuration Domains
- Auth: `Auth0` section (domain/audience/client info).
- DB: `ConnectionStrings:ChronicisDb`.
- AI: `AzureOpenAI` settings.
- Storage: `BlobStorage` settings.
- External providers: `ExternalLinks` with open API + blob provider options.
- Admin policy: `SysAdmin` identifiers.
- Logging: `Serilog` and `Logging` sections.

### 7.2 Client Configuration Domains
- API endpoint base via `ApiBaseUrl`.
- OIDC/Auth0 settings in `wwwroot/appsettings*.json`.
- Optional SysAdmin hints for client-side admin gating behavior.

## 8) Architectural Conventions and Test Enforcement

### 8.1 Test Project Layout
- `tests/Chronicis.Api.Tests`
- `tests/Chronicis.Client.Tests`
- `tests/Chronicis.Shared.Tests`
- `tests/Chronicis.ArchitecturalTests`

### 8.2 Convention Enforcement (`ArchitecturalConventionTests`)
- Service classes are expected to implement interfaces.
- Service interfaces must follow `I*` naming.
- `Task`-returning service methods must use `Async` suffix.
- DTO/model/enum conventions are enforced via reflection-based assertions.
- Extension/utility static-class patterns are verified.
- Client/API naming conventions for service families are validated.

### 8.3 Verification Workflow
- Repo verification script (`scripts/verify.ps1`) orchestrates format/build/test and coverage validation gates.

### 8.4 Targeted Architecture Regression Suites
- Dedicated suites exist for hierarchy, external-link orchestration, world membership/invitation policy behavior, and client tree-state decomposition.
- These suites complement broad unit coverage by protecting high-coupling architectural seams that previously carried duplicated logic.

## 9) Architectural Strengths and Tradeoffs

### 9.1 Strengths
- Clear shared-kernel boundary (`Shared`) reduces DTO/model duplication.
- Service-interface-first architecture improves testability and controlled composition.
- Client decomposition separates page orchestration (viewmodels) from transport/state concerns.
- API splits high-variance concerns into dedicated services (hierarchy, docs, external providers, AI, health).
- Explicit conventions are machine-enforced in architectural tests.

### 9.2 Notable Tradeoffs
- API controllers are orchestration-only (no direct `ChronicisDbContext` injection), while most persistence/query behavior remains centralized in services with direct `DbContext` usage.
- Architecture combines both first-class Session entities and legacy session article patterns, increasing compatibility complexity.
- Public and authenticated read models coexist and require duplicated access-rule rigor across service/controller boundaries.

### 9.3 Guiding Refactor Principles
- Prefer consolidating shared algorithms over adding parallel implementations.
- Preserve public contracts while decomposing internals.
- Prioritize explicit boundaries and testability over clever coupling shortcuts.
- Favor incremental refactors that reduce blast radius without forcing endpoint contract rewrites.

## 10) Architecture Governance

### 10.1 Ownership Model
- API data-access policy owner:
- Primary owner: `API Platform Lead`.
- Supporting owners: maintainers of `src/Chronicis.Api/Services` and `src/Chronicis.Api/Controllers`.
- Decision authority: data-access boundary exceptions and policy interpretation.
- Session domain architecture owner:
- Primary owner: `Campaign and Session Domain Lead`.
- Supporting owners: maintainers of `SessionService`, `PublicWorldService`, and session-related API contracts.
- Decision authority: canonical session flow and compatibility boundary scope.
- Read-policy architecture owner:
- Primary owner: `Read Policy and Public Access Lead`.
- Supporting owners: maintainers of `PublicController`, `PublicWorldService`, and authenticated read services.
- Decision authority: shared policy inputs and parity decisions.

### 10.2 API Data-Access Policy by Component Type
- Controller (`src/Chronicis.Api/Controllers/*`):
- Allowed: HTTP validation, status mapping, orchestration via service interfaces.
- Not allowed: direct `ChronicisDbContext` injection, EF query composition, `SaveChanges*` calls.
- Write orchestration service (`src/Chronicis.Api/Services/*` write paths):
- Allowed: domain-state mutation, transactional coordination, persistence via owned boundary.
- Not allowed: cross-domain persistence orchestration that bypasses service boundaries.
- Read projection service (`src/Chronicis.Api/Services/*` read paths):
- Allowed: projection-only queries, `AsNoTracking` reads, DTO assembly.
- Not allowed: tracked entity mutation, `SaveChanges*`, mixed write behavior.
- Provider adapter/service (`src/Chronicis.Api/Services/ExternalLinks/*`, provider settings paths):
- Allowed: external provider orchestration and persistence through repository abstractions.
- Not allowed: direct provider metadata persistence via `ChronicisDbContext` in adapter orchestration code.
- Health/infrastructure service:
- Allowed: health probes, diagnostics, infrastructure status reads.
- Not allowed: business-domain writes.

### 10.3 Classification Decision Tree for New API Code
- If code is an HTTP entry point, place it in a controller and call service interfaces only.
- If code mutates domain state, place it in a write orchestration service.
- If code only assembles response projections, place it in a read projection service.
- If code talks to external providers and world-provider metadata, place it behind provider adapters and repositories.
- If placement is ambiguous, architecture owner decides and records the decision.

### 10.4 Policy Exception Rules
- Every exception must include: component, owner, rationale, expiry date, and closure ticket.
- Exceptions are temporary and must be reviewed each release.
- Unowned or expired exceptions are treated as policy violations.

### 10.5 Architecture Map (Entry Points and Dependency Seams)
- Authenticated API entry points with architecture impact:
- `ArticlesController`, `WorldsController`, `DashboardController`, `SearchController`, `SessionsController`.
- Public API entry point with architecture impact:
- `PublicController` (backed by `PublicWorldService`).
- Shared policy/traversal seam:
- `ArticleHierarchyService` is shared between public and authenticated traversal/breadcrumb logic.
- `ReadAccessPolicyService` is shared between public and authenticated read-path policy evaluation.
- Persistence boundary seam:
- `ChronicisDbContext` is directly injected across many services and controllers.
- Existing repository seam:
- `IResourceProviderRepository` / `ResourceProviderRepository` is the primary explicit repository abstraction path.

### 10.6 Current Implementation Inventory (As Of 2026-04-27)
- Data-access boundaries:
- Services with direct `ChronicisDbContext` field injection: `33`.
- Controllers with direct `ChronicisDbContext` field injection: `0`.
- Session model:
- Legacy `ArticleType.Session` references in API controllers/services (excluding migrations/tests): `2`.
- Distribution: `ArticleValidationService` (`1`), `PublicWorldService` (`1`).
- Legacy `ArticleType.Session` references in client source (`.cs` + `.razor`): `7`.
- Distribution: `TutorialPageTypes` (`1`), `ArticleMetadataDrawer` (`2`), `QuestDrawer` (`2`), `TreeNode` (`1`), `TreeDataBuilder` (`1`).
- Access-policy enforcement:
- Public visibility/public slug rule condition hits in key read-path services/controllers (`PublicWorldService`, `WorldService`, `SessionService`, `SummaryService`, `PublicController`): `48`.
- Shared read-policy consumers:
- `PublicWorldService`, `ArticleService`, `ArticleDataAccessService`, `SummaryAccessService`, `SearchReadService`.
- Logging hygiene (API):
- `LogDebug*` call sites: `0`.
- `LogInformation*` call sites: `0`.
- Direct non-sanitized `LogWarning/LogError/LogCritical/LogTrace` call sites: `0`.

### 10.7 Measurement Commands (Repeatable)
```powershell
# Services with direct DbContext fields
rg -l "private readonly\s+ChronicisDbContext\b|private readonly\s+.*\s+_db\b" src/Chronicis.Api/Services

# Controllers with direct DbContext fields
rg -l "private readonly\s+ChronicisDbContext\b|private readonly\s+.*\s+_db\b" src/Chronicis.Api/Controllers

# Legacy session references in active API code (exclude migrations)
rg -n "ArticleType\.Session\b" src/Chronicis.Api/Services src/Chronicis.Api/Controllers --glob "!**/Migrations/**"

# Legacy session references in active client code
rg -n "ArticleType\.Session\b" src/Chronicis.Client

# Public-policy rule hotspots in key read-path services/controllers
rg -n "ArticleVisibility\.Public|IsPublic|PublicSlug" `
  src/Chronicis.Api/Services/PublicWorldService.cs `
  src/Chronicis.Api/Services/WorldService.cs `
  src/Chronicis.Api/Services/SessionService.cs `
  src/Chronicis.Api/Services/SummaryService.cs `
  src/Chronicis.Api/Controllers/PublicController.cs

# Shared read-policy layer consumers
rg -n "IReadAccessPolicyService|ApplyPublic|ApplyAuthenticated" `
  src/Chronicis.Api/Services/PublicWorldService.cs `
  src/Chronicis.Api/Services/ArticleService.cs `
  src/Chronicis.Api/Services/ArticleDataAccessService.cs `
  src/Chronicis.Api/Services/SummaryAccessService.cs `
  src/Chronicis.Api/Services/SearchReadService.cs

# Logging hygiene checks (API source)
rg -n "\.Log(Debug|Information)(Sanitized)?\(" src/Chronicis.Api --glob "**/*.cs"
rg -n "\.Log(Warning|Error|Critical|Trace)\(" src/Chronicis.Api --glob "**/*.cs"
```

### 10.8 Compliance Metrics and Release Gates
- Data-access boundary metric:
- Measure: number of controllers/services directly injecting `ChronicisDbContext`.
- Gate: count must not increase release-over-release; temporary exceptions must be tracked.
- Session architecture metric:
- Measure: number of legacy `ArticleType.Session` references outside migrations/tests.
- Gate: count must stay at or below the retirement baseline and trend to full retirement.
- Access-policy parity metric:
- Measure: duplicated visibility/public-slug rule paths outside shared policy inputs.
- Gate: duplicated rule logic must trend down; divergences block release sign-off.
- Logging hygiene metric:
- Measure: API direct `LogDebug*`/`LogInformation*` call count and direct non-sanitized logger call count.
- Gate: both counts remain at `0`; any increase blocks release.

### 10.9 Data-Access Boundary Operating Model
- Domain boundary unit:
- One API domain boundary consists of controller orchestration paths, owning services, targeted tests, and exception-ledger coverage.
- API domain coverage groups:
- Content and graph domains: `Articles`, `Worlds`, `Public read path`, `Search`.
- Collaboration and progression domains: `Campaigns`, `Arcs`, `Sessions`, `WorldMembership`, `WorldInvitation`, `WorldDocuments`.
- Supporting domains: `Summary`, `Admin`, `Tutorial`, `Health`, and edge endpoints.
- Change checklist:
- Classify each new or changed code path to a policy type from Section `10.2`.
- Keep controllers free of direct `ChronicisDbContext` usage.
- Keep persistence and projection logic inside owning services.
- Add or update boundary and regression tests for touched domains.
- Run repo verification (`scripts/verify.ps1`) before merge.
- Release criteria:
- No new controller-level `DbContext` usage.
- No unmanaged policy exceptions.
- Domain behavior remains contract-compatible unless versioned API changes are explicitly approved.

### 10.10 Data-Access Exception Ledger
- Required exception fields are enforced by Section `10.4`: component, owner, rationale, expiry date, closure ticket.
- Active exceptions: none.
- Baseline statement: no controller-level `DbContext` exceptions are permitted.

### 10.11 Domain Boundary Coverage
- Content and graph domains are implemented through service-owned boundaries for `Articles`, `Worlds`, `Public read path`, and `Search`.
- Collaboration and progression domains are implemented through service-owned boundaries for `Campaigns`, `Arcs`, `Sessions`, `WorldMembership`, `WorldInvitation`, and `WorldDocuments`.
- Supporting domains are implemented through service-owned boundaries for `Summary`, `Admin`, `Tutorial`, `Health`, and edge endpoints.

### 10.12 Session Model Canonicalization
- Canonical session model:
- `Session` entity (`Chronicis.Shared.Models.Session`) is the only model for new session workflows.
- `ArticleType.Session` is non-canonical and retained only for constrained legacy-model interop.
- Legacy reference boundary policy:
- Allowed API files:
- `src/Chronicis.Api/Services/ArticleValidationService.cs` (rejection guard for new legacy session articles)
- `src/Chronicis.Api/Services/PublicWorldService.cs` (session virtual-group projection only; legacy URL compatibility retired)
- Allowed client files:
- `src/Chronicis.Client/Components/Admin/TutorialPageTypes.cs`
- `src/Chronicis.Client/Components/Articles/ArticleMetadataDrawer.razor`
- `src/Chronicis.Client/Components/Quests/QuestDrawer.razor.cs`
- `src/Chronicis.Client/Models/TreeNode.cs`
- `src/Chronicis.Client/Services/Tree/TreeDataBuilder.cs`
- Freeze rule:
- No new `ArticleType.Session` references may be introduced outside the allowlist.
- Transition states:
- `Coexistence`: canonical and legacy models both exist; all new write paths remain canonical.
- `Compatibility-only`: legacy references are read/interop only; no legacy write paths are allowed.
- `Retirement`: legacy references and data paths removed after parity and migration exit criteria are satisfied.
- Current state:
- Retired public legacy session-prefix URL compatibility shims.
- Guardrails:
- `tests/Chronicis.ArchitecturalTests/SessionModelGuardrailTests.cs` enforces:
- no expansion of boundary file set for API/client.
- no increase above baseline legacy-reference counts (`API <= 2`, `Client <= 7`).

### 10.13 Unified Access-Policy Architecture
- Shared policy contract:
- `IReadAccessPolicyService` defines canonical read-policy inputs for:
- public world lookup, public article visibility, tutorial readability, and authenticated world/article/campaign/arc access.
- Shared implementation:
- `ReadAccessPolicyService` centralizes policy evaluation and query composition for both anonymous and authenticated reads.
- Migrated read-path consumers:
- `PublicWorldService`, `ArticleService`, `ArticleDataAccessService`, `SummaryAccessService`, and `SearchReadService`.
- Separation of concerns:
- Policy decisions are evaluated once in the policy layer.
- Projection assembly (DTO/tree/path materialization) remains in owning read services.
- Validation:
- `tests/Chronicis.Api.Tests/Services/ReadAccessPolicyServiceTests.cs` covers policy matrix behavior.
- Service-level regressions validate adoption in public/auth read paths.

### 10.14 Read-Model Parity Hardening
- Shared projection seam:
- `ArticleReadModelProjection.ArticleDetail` is the canonical `ArticleDto` read projection for parity-sensitive public/auth article detail reads.
- Shared path-resolution seam:
- `ArticleSlugPathResolver.ResolveAsync` is the canonical slug-chain traversal primitive used by:
- `PublicWorldService.GetPublicArticleAsync`
- `ArticleService.TryResolveWorldArticleByPathAsync`
- `ArticleService.TryResolveTutorialArticleByPathAsync`
- Parity validation:
- `tests/Chronicis.Api.Tests/Services/ReadModelParityTests.cs` compares public/auth read behavior under equivalent visibility constraints.
- Intentional divergence boundaries (test-protected):
- authenticated private-owner access is broader than anonymous public reads.
- Release policy:
- any unplanned public/auth read divergence is a release blocker.
- any planned divergence must be documented and protected by regression tests.

### 10.15 Architecture Test Guardrails
- Data-access boundary guardrails:
- `tests/Chronicis.ArchitecturalTests/ArchitectureGuardrailTests.cs` enforces that API controllers do not directly depend on `ChronicisDbContext` (constructor, field, or property).
- Read-policy continuity guardrails:
- the same suite enforces continued `IReadAccessPolicyService` injection for key read services:
- `PublicWorldService`, `ArticleService`, `ArticleDataAccessService`, `SummaryAccessService`, `SearchReadService`.
- Parity seam guardrails:
- the same suite enforces continued shared parity seam usage in public/auth path reads:
- `ArticleSlugPathResolver.ResolveAsync`
- `ArticleReadModelProjection.ArticleDetail`
- Canonical session-flow guardrails:
- `SessionService` is guarded against reintroducing legacy `ArticleType.Session` write behavior.
- Runtime regression coverage:
- `tests/Chronicis.Api.Tests/Services/SessionServiceTests.cs` includes canonical flow regression assertions for session creation.
- Release policy:
- any guardrail failure is a release blocker and must be resolved or explicitly redesigned with updated tests/docs.

### 10.16 Rollout and Risk Control
- Rollout control model:
- staged revision rollout with checkpoints at `10%`, `50%`, and `100%` traffic.
- progression to the next stage is blocked unless the current stage checkpoint passes.
- Checkpoint execution:
- `scripts/rollout-checkpoint.ps1` evaluates:
- API readiness (`/health/ready`);
- system health (`/health/status`);
- operational indicators (`p95LatencyMs`, `errorRatePercent`, `authDenialsPercent`, `dataConsistencyDelta`) supplied from observability dashboards and validation checks.
- Checkpoint configuration:
- `scripts/rollout-checkpoint.sample.json` defines stage name and rollback thresholds.
- Result semantics:
- exit code `0` = proceed;
- exit code `1` = rollback.
- Operational process:
- `docs/ROLLOUT_RUNBOOK.md` defines stage sequence, rollback actions, and required evidence per checkpoint.
- `docs/OBSERVABILITY.md` defines how to collect and map indicator values into checkpoint inputs.
- Release policy:
- any failed checkpoint or threshold breach is a release blocker.
- rollback to the previous stable revision is required before further progression.

### 10.17 Closure and Debt Retirement
- Retirement actions:
- Removed public read-path compatibility shims that resolved legacy session-prefixed URLs for root `SessionNote` articles.
- Public path generation now emits canonical root-note paths with no legacy session slug prefix insertion.
- Public/auth parity now expects legacy session-prefixed paths to fail in both models.
- Baseline updates:
- Architecture inventory and architecture-guardrail baselines were updated to the post-shim retirement counts.
- Follow-up backlog (non-blocking):
- Reduce remaining API/client `ArticleType.Session` references to full retirement.
- Remove/rename remaining legacy model comments and helper names after DTO/public-contract migration windows.
- Re-run legacy-data validation checks in staged rollout checkpoints before final boundary removal.

### 10.18 Logging Hygiene
- Logging policy for API source:
- no `LogInformation*` calls.
- no `LogDebug*` calls.
- logger calls must use `*Sanitized` extension methods.
- Extension support:
- `LoggerExtensions` includes warning/error/critical/trace sanitized overloads, including exception variants required by API call sites.
- Architecture guardrails:
- `tests/Chronicis.ArchitecturalTests/LoggingHygieneTests.cs` enforces:
- zero debug/information logger calls in API source;
- zero direct non-sanitized warning/error/critical/trace logger calls in API source.
- Release policy:
- any logging hygiene guardrail failure is a release blocker.

### 10.19 URL Restructure Release
- Scope: Phases 01–07 of the `url-redesign` branch (~43 files changed).
- Migration: `UrlRestructure_SlugFoundations` — adds `Slug` columns to `Campaign`, `Arc`, `Session`, and `WorldMap`; renames `World.PublicSlug` to `World.Slug`; adds sibling-unique indexes per entity.
- Retired URL shapes:
- `/w/{publicSlug}` and `/w/{publicSlug}/{*path}` — public world/article viewer.
- `/article/{*path}` — authenticated article path routing.
- `/world/{guid}`, `/campaign/{guid}`, `/arc/{guid}`, `/session/{guid}` — GUID-based entity detail routes.
- `/world/{guid}/maps` and `/world/{guid}/maps/{guid}` — GUID-based map routes.
- Canonical URL scheme (all entity detail paths):
- `/{worldSlug}` — world detail.
- `/{worldSlug}/{campaignSlug}` — campaign detail.
- `/{worldSlug}/{campaignSlug}/{arcSlug}` — arc detail.
- `/{worldSlug}/{campaignSlug}/{arcSlug}/{sessionSlug}` — session detail.
- `/{worldSlug}/{*articlePath}` — wiki article or session note.
- `/{worldSlug}/maps` — map listing.
- `/{worldSlug}/maps/{mapSlug}` — map detail.
- Reserved slugs (`dashboard`, `settings`, `w`, `article`, etc.) redirect to `/dashboard` on the client.
- Routing implementation: `PathResolver` (`@page "/{*Path}"`) resolves all entity URLs; `PathsController` (`GET /api/paths/resolve/{*path}`) serves the server-side resolution.
- Deleted components: `Articles.razor`, `PublicWorldPage.razor`, `PublicWorldPageViewModel.cs`.
- Architecture guardrail added: `ClientPages_MustNotContainGuidBasedPageDirectives` in `ArchitectureGuardrailTests.cs`.
- Anonymous/authenticated parity: both audiences share identical URL shapes; `IReadAccessPolicyService` governs content visibility.

## 11) Out of Scope
- `Chronicis.CaptureApp` architecture is intentionally excluded.
