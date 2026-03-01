# Chronicis Architecture Inventory

Last reviewed: 2026-02-28

## 1) Scope
- Projects covered:
- `src/Chronicis.Api`
- `src/Chronicis.Client`
- `src/Chronicis.Shared`
- Purpose:
- Capture the implemented architecture (composition roots, boundaries, dependencies, persistence model, integration adapters, and conventions).
- Exclusions:
- `src/Chronicis.CaptureApp` (explicitly out of scope for this inventory).

## 2) System Topology

### 2.1 Runtime Components
- Browser-hosted Blazor WebAssembly app (`Chronicis.Client`) serves UI and user interaction workflows.
- ASP.NET Core API (`Chronicis.Api`) provides authenticated and public endpoints.
- SQL Server (via EF Core) stores application state and relational graph.
- Azure Blob Storage stores world documents and inline article images.
- Azure OpenAI provides summary generation.
- External content providers (Open5e + blob-backed SRD/ROS datasets) provide third-party reference content.

### 2.2 Dependency Direction (Compile-Time)
```text
Chronicis.Client  ---->  Chronicis.Shared
Chronicis.Api     ---->  Chronicis.Shared
Chronicis.Shared  ---->  (no project references)
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
- Public read model module:
- `PublicWorldService` for anonymous world/article/document access projections.
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
- Quest entities are arc-scoped with quest-update timeline entities and optional session reference.
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
- Worlds, world links, world documents, campaigns, arcs, sessions, articles, search, external links, quests/updates, users/dashboard, public sharing, admin/tutorials, summaries, resource providers, health.
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
- `App.razor` + custom `ChronicisRouteView` determine layout/auth rendering behavior.
- Page layer:
- Route-bound pages in `Pages/*` are top-level screens.
- Component layer:
- Reusable UI and workflow components in `Components/*`.
- ViewModel layer:
- `ViewModels/*` use `INotifyPropertyChanged` (`ViewModelBase`) for page state and orchestration.
- Service layer:
- API clients, app state, drawer/shortcut coordination, markdown/render helpers.
- Infrastructure abstraction layer:
- `Infrastructure/*` adapters isolate navigation/dialog/title/notification concerns from viewmodels.
- JS interop layer:
- `wwwroot/js/*` modules provide editor/shortcut/upload/autocomplete interop.

### 4.3 Routing and Layout Architecture
- `ChronicisRouteView` inspects page metadata (`@layout`, `[Authorize]`) to choose layout and avoid auth-layout flicker.
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
- Navigation/interaction interop modules:
- `keyboardShortcuts.js`
- `publicWikiLinks.js`
- `questEditor.js`
- Auxiliary UX/diagnostic modules:
- `emojiPickerInterop.js`
- `rum.js`

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
- Questing: `Quest`, `QuestUpdate`
- Configuration/content templates: `SummaryTemplate`, `TutorialPage`, `ResourceProvider`, `WorldResourceProvider`

### 5.3 Contract/DTO Layer
- DTO modules in `DTOs/*` provide API contract boundaries by domain area:
- articles, worlds, campaigns/arcs/sessions, links/external links, summaries, search, dashboard, admin/tutorials, health, users, characters, resource providers.
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
- Most API domains use direct DbContext access in services/controllers, while repository abstraction is selectively applied (resource providers only).
- Architecture combines both first-class Session entities and legacy session article patterns, increasing compatibility complexity.
- Public and authenticated read models coexist and require duplicated access-rule rigor across service/controller boundaries.

### 9.3 Guiding Refactor Principles
- Prefer consolidating shared algorithms over adding parallel implementations.
- Preserve public contracts while decomposing internals.
- Prioritize explicit boundaries and testability over clever coupling shortcuts.
- Favor incremental refactors that reduce blast radius without forcing endpoint contract rewrites.

## 10) Out of Scope
- `Chronicis.CaptureApp` architecture is intentionally excluded.
