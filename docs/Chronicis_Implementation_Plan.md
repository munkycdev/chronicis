# Chronicis Implementation Plan - Complete Reference

**Version:** 2.3 | **Date:** December 21, 2025  
**Purpose:** Complete phase-by-phase implementation guide with detailed specifications

**CHANGES IN v2.3:**
- Phase 1.5.1b: **COMPLETE** - World & Campaign Management APIs
- Phase 1.5.1b: New endpoints for World CRUD operations
- Phase 1.5.1b: New endpoints for Campaign CRUD and member management
- Phase 1.5.1b: Auto-creation of Act 1 and SharedInfoRoot when campaign created
- Phase 1.5.1b: See docs/phase-1.5/Phase_1_5_1b_World_Campaign_APIs.md for full details

**CHANGES IN v2.2:**
- Phase 1.5.1a: **COMPLETE** - Guid ID Migration & Multi-User Foundation
- Phase 1.5.1a: All entity IDs migrated from int to Guid
- Phase 1.5.1a: New entities added: World, Campaign, CampaignMember
- Phase 1.5.1a: New enums: ArticleType, CampaignRole, ArticleVisibility
- Phase 1.5.1a: Property renames: CreatedDate → CreatedAt, ModifiedDate → ModifiedAt
- Phase 1.5.1a: EF Core migration created and applied to dev database
- Phase 1.5.1a: See docs/phase-1.5/Phase_1_5_1a_Guid_Migration.md for full details

**CHANGES IN v2.1:**
- Phase 10: **COMPLETE** - Hierarchical URL Paths
- Phase 10: Articles now use full hierarchical paths in URLs (e.g., `/article/sword-coast/waterdeep/castle-ward`)
- Phase 10: Slug system with auto-generation from titles and user confirmation for changes
- Phase 10: Path-based article lookup via `/api/articles/by-path/{*path}` endpoint
- Phase 10: Database migration for Slug column with unique constraints per parent scope
- Phase 10: Automatic slug backfilling for existing articles
- Phase 10: Hierarchical breadcrumb navigation with clickable paths
- All Phase 10 features tested and working end-to-end

**CHANGES IN v2.0:**
- Phase 12: **IN PROGRESS** - Azure deployment completed successfully
- Phase 12: Azure Static Web Apps deployment with GitHub Actions CI/CD
- Phase 12: Azure SQL Database provisioned and migrations applied
- Phase 12: Auth0 production configuration with Azure callback URLs
- Phase 12: **Critical Fix:** Azure SWA auth token interception workaround using X-Auth0-Token header
- Phase 12: Downgraded to .NET 9 for Azure SWA compatibility
- Phase 12: SPA routing configured via staticwebapp.config.json
- All deployment features tested and working end-to-end

---

## Quick Navigation

- [Project Context](#project-context) | [Phase Overview](#phase-overview)
- [Phase 0-8 Summary](#phases-0-8-summary) | [Phase 9](#phase-9) | [Phase 9.5](#phase-9-5) | [Phase 10](#phase-10) | [Phase 11](#phase-11) | [Phase 12](#phase-12)
- [Appendices](#appendices)

---

## Project Context

**What:** Web-based knowledge management for D&D campaigns  
**Stack:** Blazor WASM + Azure Functions + Azure SQL + MudBlazor  
**Timeline:** 16 weeks (12 phases)  
**Approach:** Local dev → Test → Deploy to Azure when stable

**Live URL:** https://ambitious-mushroom-015091e1e.3.azurestaticapps.net

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
| 10 | Hierarchical URLs | 1 | ✅ **COMPLETE** | Path-based URLs, slug system, breadcrumb navigation |
| 1.5.1a | Guid ID Migration | 0.5 | ✅ **COMPLETE** | Multi-user foundation, World/Campaign entities |
| 1.5.1b | World/Campaign APIs | 0.5 | ✅ **COMPLETE** | CRUD endpoints, member management, auto-creation |
| 11 | Icons & Polish | 1 | 📋 Pending | Custom icons, final touches |
| 12 | Testing & Deploy | 2 | 🔄 **IN PROGRESS** | Azure deployment ✅, E2E tests pending |

---

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
- GET /api/articles/{id}/backlinks
- GET /api/hashtags/{name}/preview

### Phase 8: AI Summary Generation
- Azure OpenAI integration with GPT-4.1-mini
- AISummaryService with cost estimation
- Configuration-driven prompts
- Pre-generation cost transparency
- AISummarySection component with collapsible UI
- Copy, regenerate, clear actions
- Application Insights logging
- POST /api/articles/{id}/summary/generate
- GET /api/articles/{id}/summary

---

## Phase 9: Advanced Search & Content Discovery

**Status:** ✅ **COMPLETE** (v1.8)

**Goal:** Implement full-text content search across article bodies and hashtags with global search interface

**Completed:** November 28, 2025

### Overview

Phase 9 adds powerful content search that complements the existing title-only tree search. Users can now search for content anywhere in their campaign notes and see grouped results by match type.

### Backend Implementation

**Enhanced ArticleSearchFunction:**
- Searches across titles, bodies, AND hashtags
- Returns results grouped by match type
- Generates context snippets (200 characters) with search term
- Builds breadcrumb paths for navigation
- Limits to 20 results per category (60 total max)

**API Endpoint:**
```
GET /api/articles/search?query={term}
```

### Success Criteria

1. ✅ Global search box appears in app header
2. ✅ Results grouped by match type (Title, Content, Hashtag)
3. ✅ Query terms highlighted in yellow
4. ✅ Click to navigate to article
5. ✅ All build warnings resolved

---

## Phase 9.5: Authentication Architecture Refactoring

**Status:** ✅ **COMPLETE** (v1.9)

**Goal:** Centralize authentication handling to eliminate repetitive code

**Completed:** December 1, 2025

### Overview

Phase 9.5 refactors authentication across both backend (Azure Functions) and frontend (Blazor WASM) to use centralized patterns instead of per-function/per-service authentication calls.

### Backend Solution: Global Middleware

- `AuthenticationMiddleware.cs` implements `IFunctionsWorkerMiddleware`
- `[AllowAnonymous]` attribute for public endpoints
- `FunctionContextExtensions` for easy user access (`context.GetRequiredUser()`)

### Frontend Solution: Centralized HttpClient

- `ChronicisAuthHandler` (DelegatingHandler) auto-attaches bearer tokens
- `IHttpClientFactory` with named client pattern
- All services use single named client with automatic token attachment

---

## Phase 10: Hierarchical URL Paths

**Status:** ✅ **COMPLETE**

**Completed:** December 15, 2025  
**Implementation Time:** ~6 hours (including EF migration troubleshooting)

**Goal:** Implement hierarchical URL paths for articles with slug-based navigation

### Overview

Phase 10 replaces single-slug URLs (`/article/waterdeep`) with full hierarchical paths (`/article/sword-coast/waterdeep/castle-ward`), providing unambiguous deep linking and better bookmarkability.

### Backend Implementation

**Data Model:**
- Added `Slug` property to Article entity (string, required, max 200 chars)
- Updated all DTOs to include slug
- `SlugGenerator` utility class for slug creation and validation

**Database Migration** (`20251204213028_AddArticleSlugColumn`):
1. Adds Slug column (nullable initially)
2. Backfills slugs from existing article titles
3. Makes Slug required
4. Creates unique indexes:
   - `IX_Articles_Slug_Root` - for root articles (ParentId IS NULL)
   - `IX_Articles_ParentId_Slug` - for child articles (ParentId IS NOT NULL)

**ArticleService Enhancements:**
- `GetArticleByPathAsync` - walks tree using slugs
- `IsSlugUniqueAsync` - validates uniqueness within parent scope
- `GenerateUniqueSlugAsync` - creates unique slugs with numeric suffixes if needed
- `BuildArticlePathAsync` - builds full hierarchical path

**API Endpoints:**
- `CreateArticle` - auto-generates slugs, validates uniqueness, supports custom slugs
- `UpdateArticle` - handles slug changes with validation
- `GetArticleByPath` - NEW endpoint: `GET /api/articles/by-path/{*path}`

### Frontend Implementation

**Routing:**
- Changed from `/article/{ArticleSlug}` to `/article/{*Path}`
- Path-based loading via `GetArticleByPathAsync`

**Navigation:**
- ArticleTreeView builds full hierarchical paths on selection
- ArticleDetail updates URL when title changes (with user confirmation)
- Breadcrumbs use hierarchical paths (each level clickable)

**Slug Management:**
- Auto-generated from titles
- User confirmation dialog when title changes affect slug
- Shows current vs. suggested slug
- Warns about breaking old links

### Key Features

1. **Hierarchical URLs** - Full context in URL path
2. **Deep Linking** - Bookmarkable multi-level paths
3. **Clickable Breadcrumbs** - Each level navigable
4. **Slug Uniqueness** - Per parent scope (siblings can't duplicate)
5. **Automatic Backfilling** - Migration handles existing articles

### URL Examples

- Root: `/article/waterdeep`
- Two-level: `/article/sword-coast/waterdeep`
- Three-level: `/article/sword-coast/waterdeep/castle-ward`
- Duplicate handling: `/article/waterdeep`, `/article/north/waterdeep-2`

### Success Criteria

All criteria met and tested:
1. ✅ URLs show full hierarchical paths
2. ✅ Breadcrumbs clickable with correct paths
3. ✅ Direct URLs (deep links) work
4. ✅ Browser back/forward works
5. ✅ Title changes prompt slug update
6. ✅ Slugs unique within parent
7. ✅ Tree navigation uses paths
8. ✅ Migration backfills existing articles

### Files Modified

**Backend:**
- `Shared/Models/Article.cs` - Added Slug
- `Shared/DTOs/ArticleDTOs.cs` - Slug in all DTOs
- `Shared/Utilities/SlugGenerator.cs` - NEW
- `Data/ChronicisDbContext.cs` - Slug indexes
- `Services/ArticleService.cs` - Path resolution
- `Functions/CreateArticle.cs` - Slug generation
- `Functions/UpdateArticle.cs` - Slug updates
- `Functions/GetArticleByPath.cs` - NEW

**Frontend:**
- `Utilities/ArticlePathBuilder.cs` - NEW
- `Services/ArticleApiService.cs` - Path-based lookup
- `Pages/Articles.razor` - Path routing
- `Components/Articles/ArticleDetail.razor` - Slug confirmation
- `Components/Articles/ArticleTreeView.razor` - Path navigation

---

## Phase 11: Custom Icons & Visual Enhancements

**Status:** 📋 Pending

**Goal:** Allow custom emoji icons and final polish

### Features

- EmojiPicker component
- Icon selection in inline editor (near title)
- Display icons in tree view
- Icons in breadcrumbs
- Large icon in article header
- Smooth animations throughout

---

## Phase 12: Testing, Performance & Deployment

**Status:** 🔄 **IN PROGRESS**

**Goal:** Ensure quality, optimize, deploy to production

### Azure Deployment - ✅ COMPLETE

**Completed:** December 4, 2025  
**Implementation Time:** ~4 hours (including troubleshooting auth issues)

#### Azure Resources Created

| Resource | Name | Details |
|----------|------|---------|
| Resource Group | rg-chronicis-dev | West US 2 |
| SQL Server | sql-chronicis-dev | Basic tier |
| SQL Database | chronicis-db | 5 DTU |
| Static Web App | swa-chronicis-dev | Free tier, GitHub Actions CI/CD |

**Live URL:** https://ambitious-mushroom-015091e1e.3.azurestaticapps.net

#### Configuration

**App Settings (Azure Static Web Apps):**
- `Auth0__Domain` - Auth0 tenant domain
- `Auth0__Audience` - API audience identifier
- `Auth0__ClientId` - Auth0 application client ID
- `AzureOpenAI__Endpoint` - Azure OpenAI endpoint URL
- `AzureOpenAI__ApiKey` - Azure OpenAI API key
- `AzureOpenAI__DeploymentName` - GPT model deployment name
- `AzureOpenAI__MaxInputTokens` - Token limit for input
- `AzureOpenAI__MaxOutputTokens` - Token limit for output
- `ConnectionStrings__ChronicisDb` - Azure SQL connection string

**Auth0 Configuration:**
- Added Azure callback URL to Allowed Callback URLs
- Added Azure URL to Allowed Logout URLs
- Added Azure URL to Allowed Web Origins

#### Critical Issue Resolved: Azure SWA Auth Token Interception

**Problem:** Azure Static Web Apps' managed functions automatically intercept and replace the standard `Authorization` header with their own internal HS256 token. This caused Auth0's RS256 JWT tokens to be replaced before reaching the Azure Function.

**Symptoms:**
- Browser sent correct RS256 token (verified in Network tab)
- Azure Function received different HS256 token (376 chars vs 800+ chars)
- JWT validation failed with "kid missing" error

**Solution:** Use a custom header to bypass SWA's auth interception:

**Client (ChronicisAuthHandler.cs):**
```csharp
// Use custom header to bypass Azure SWA's auth interception
request.Headers.Add("X-Auth0-Token", token.Value);

// Also set standard header for local development
request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
```

**API (Auth0AuthenticationHelper.cs):**
```csharp
// Check for custom header first (Azure SWA workaround)
if (req.Headers.TryGetValues("X-Auth0-Token", out var customTokenValues))
{
    token = customTokenValues.FirstOrDefault();
}

// Fall back to standard Authorization header (local dev)
if (string.IsNullOrEmpty(token))
{
    // ... standard Authorization header handling
}
```

**Key Learning:** Azure SWA only intercepts the standard `Authorization` header - custom headers pass through unchanged.

#### Other Deployment Fixes

**.NET Version:**
- Downgraded from .NET 10 to .NET 9 (Azure SWA doesn't support .NET 10 yet)
- Updated all `.csproj` files and `global.json`

**SPA Routing:**
- Added `staticwebapp.config.json` for client-side routing
- Navigation fallback to `index.html` for Blazor routes
- Excluded static files and API routes from fallback

**Dynamic Auth URLs:**
- Updated `Program.cs` to use `builder.HostEnvironment.BaseAddress`
- Auth redirect URLs work for both localhost and Azure

### Remaining Phase 12 Work

#### Testing (Pending)
- [ ] Unit tests for Article CRUD
- [ ] Unit tests for hashtag parsing
- [ ] Unit tests for AI summary service
- [ ] Unit tests for search functionality
- [ ] Unit tests for authentication middleware
- [ ] Integration tests for API endpoints
- [ ] Manual test plan execution

#### Performance Optimizations (Pending)
- [ ] Add database indexes
- [ ] Query optimization with projections
- [ ] Response compression
- [ ] Caching headers

#### Monitoring (Pending)
- [ ] Set up Application Insights
- [ ] Configure alerts for errors
- [ ] Monitor DTU usage on SQL Database
- [ ] Set up cost alerts

---

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

**Azure CLI:**
```powershell
# Login
az login

# View app settings
az staticwebapp appsettings list `
    --name swa-chronicis-dev `
    --resource-group rg-chronicis-dev

# Set app setting
az staticwebapp appsettings set `
    --name swa-chronicis-dev `
    --resource-group rg-chronicis-dev `
    --setting-names "Key=Value"

# Add SQL firewall rule
az sql server firewall-rule create `
    --name RuleName `
    --resource-group rg-chronicis-dev `
    --server sql-chronicis-dev `
    --start-ip-address X.X.X.X `
    --end-ip-address X.X.X.X
```

**Deployment:**
```powershell
# Trigger manual deployment
git commit --allow-empty -m "Trigger deployment"
git push

# Run migrations against Azure SQL
$connectionString = "Server=tcp:sql-chronicis-dev.database.windows.net,1433;..."
dotnet ef database update --connection $connectionString
```

### B. Performance Targets

- **Initial Load:** < 3 seconds
- **Tree Expansion:** < 300ms
- **Article Display:** < 500ms
- **Search Results:** < 1 second
- **Auto-Save:** < 500ms
- **Hashtag Sync:** < 50ms
- **AI Summary Generation:** < 30 seconds
- **Hover Tooltip:** < 300ms

### C. Project Structure

```
chronicis/
├── src/
│   ├── Chronicis.Client/           # Blazor WASM
│   │   ├── Components/
│   │   │   ├── Articles/
│   │   │   │   ├── ArticleDetail.razor
│   │   │   │   ├── ArticleTreeView.razor
│   │   │   │   ├── BacklinksPanel.razor
│   │   │   │   ├── AISummarySection.razor
│   │   │   │   └── SearchResultCard.razor
│   │   │   └── Hashtags/
│   │   │       └── HashtagLinkDialog.razor
│   │   ├── Services/
│   │   │   ├── ArticleApiService.cs
│   │   │   ├── ChronicisAuthHandler.cs      # Auth token handler
│   │   │   ├── TreeStateService.cs
│   │   │   ├── HashtagApiService.cs
│   │   │   ├── AISummaryApiService.cs
│   │   │   └── SearchApiService.cs
│   │   ├── Pages/
│   │   │   ├── Home.razor
│   │   │   └── Search.razor
│   │   └── wwwroot/
│   │       ├── css/
│   │       ├── js/
│   │       └── staticwebapp.config.json    # SWA routing config
│   ├── Chronicis.Api/              # Azure Functions
│   │   ├── Functions/
│   │   │   ├── ArticleFunctions.cs
│   │   │   ├── ArticleSearchFunction.cs
│   │   │   ├── HashtagFunctions.cs
│   │   │   ├── BacklinkFunctions.cs
│   │   │   ├── AISummaryFunctions.cs
│   │   │   └── HealthFunction.cs
│   │   ├── Infrastructure/
│   │   │   ├── AuthenticationMiddleware.cs
│   │   │   ├── AllowAnonymousAttribute.cs
│   │   │   ├── FunctionContextExtensions.cs
│   │   │   ├── Auth0Configuration.cs
│   │   │   └── Auth0AuthenticationHelper.cs  # X-Auth0-Token support
│   │   ├── Services/
│   │   │   ├── HashtagParser.cs
│   │   │   ├── HashtagSyncService.cs
│   │   │   ├── AISummaryService.cs
│   │   │   ├── ArticleService.cs
│   │   │   └── UserService.cs
│   │   └── Data/
│   │       └── Entities/
│   └── Chronicis.Shared/           # DTOs
└── Chronicis.sln
```

### D. Troubleshooting

**Azure SWA replacing auth token:**
- Symptom: API receives HS256 token when browser sends RS256
- Cause: Azure SWA intercepts Authorization header for managed functions
- Solution: Use `X-Auth0-Token` custom header

**.NET version not supported:**
- Symptom: Build error about unsupported platform version
- Cause: Azure SWA doesn't support .NET 10 yet
- Solution: Downgrade to .NET 9 in all `.csproj` and `global.json`

**SPA routes return 404:**
- Symptom: Direct navigation to /dashboard returns 404
- Cause: Azure doesn't know to route to index.html
- Solution: Add `staticwebapp.config.json` with navigationFallback

**401 Unauthorized on API:**
- Check: Token being sent in request headers
- Check: Auth0 audience matches API configuration
- Check: `X-Auth0-Token` header is present (for Azure)
- Solution: Verify app settings in Azure portal

**Database connection failed:**
- Check: SQL Server firewall allows your IP
- Check: Connection string is correct in app settings
- Check: `ConnectionStrings__ChronicisDb` key name matches code

**App settings not taking effect:**
- Cause: Functions need redeploy to pick up new settings
- Solution: Trigger a new deployment via git push

### E. Using This Plan

**For Remaining Work:**
1. Review remaining Phase 12 tasks (testing, monitoring)
2. Consider Phase 10 (Drag & Drop) and Phase 11 (Icons)
3. Create new chat with Claude
4. Upload this plan + spec PDFs
5. Reference specific phase/task

**When Stuck:**
1. Check troubleshooting section
2. Review phase specification
3. Ask Claude in current chat
4. Check official documentation

### F. AI Tool Strategy

**Use Claude for:**
- Architecture decisions
- Phase planning
- API design
- Complex business logic
- Debugging tricky issues (like the SWA auth token issue!)
- Code review

**Use GitHub Copilot for:**
- Implementation details
- Boilerplate code
- Common patterns
- Test generation
- Refactoring

**Model Selection:**
- **Opus 4.5:** Best for complex multi-file changes, architectural decisions
- **Sonnet 4:** Good for focused implementation tasks

---

## Final Notes

**Current Status: DEPLOYED TO AZURE! 🎉**

**Live URL:** https://ambitious-mushroom-015091e1e.3.azurestaticapps.net

**What's Working:**
- ✅ User authentication via Auth0
- ✅ Article CRUD operations
- ✅ Hierarchical tree navigation
- ✅ Rich text editing with TipTap
- ✅ Hashtag system with linking
- ✅ Backlinks panel
- ✅ AI-powered summaries
- ✅ Full-text search
- ✅ Auto-save functionality

**Current Progress:**
**Phase 12 partially complete** (~90% of project)
- Phases 0-9.5: ✅ Complete
- Phase 10: 📋 Pending (Drag & Drop)
- Phase 11: 📋 Pending (Icons & Polish)  
- Phase 12: 🔄 In Progress (Deployment ✅, Testing pending)

**Key Learnings from Deployment:**
1. Azure SWA intercepts Authorization headers - use custom headers
2. .NET 10 not yet supported on Azure SWA - use .NET 9
3. SPA routing requires staticwebapp.config.json
4. App setting changes require redeployment
5. Debug logging is essential for troubleshooting remote issues

---

**Version History:**
- 2.3 (2025-12-21): Phase 1.5.1b COMPLETE - World & Campaign management APIs, member management, auto-creation logic
- 2.2 (2025-12-20): Phase 1.5.1a COMPLETE - Guid ID migration, World/Campaign/CampaignMember entities, multi-user foundation
- 2.1 (2025-12-15): Phase 10 COMPLETE - Hierarchical URL paths, slug system
- 2.0 (2025-12-04): Phase 12 deployment COMPLETE - Azure SWA, SQL, Auth0 production config, X-Auth0-Token workaround
- 1.9 (2025-12-01): Phase 9.5 COMPLETE - Auth architecture refactoring
- 1.8 (2025-11-28): Phase 9 COMPLETE - Full-text content search
- 1.7 (2025-11-27): Phase 8 COMPLETE - AI summaries with Azure OpenAI
- 1.6 (2025-11-27): Phase 7 COMPLETE - Interactive hashtags, backlinks
- 1.5 (2025-11-26): Phase 6 COMPLETE - Full hashtag system
- 1.4 (2025-11-25): Phase 5 COMPLETE - Dashboard, routing
- 1.3 (2025-11-25): Phase 5 complete implementation
- 1.2 (2025-11-24): Phase 4 complete (TipTap)
- 1.1 (2025-11-23): Updated for inline editing
- 1.0 (2025-11-18): Initial plan

---

*End of Implementation Plan*
