# Chronicis Implementation Plan - Complete Reference

**Version:** 2.0 | **Date:** December 2, 2025  
**Purpose:** Complete phase-by-phase implementation guide with detailed specifications

**CHANGES IN v2.0:**
- Phase 10: **COMPLETE** - Drag-and-Drop Reorganization
- Phase 10: HTML5 drag API via Blazor events
- Phase 10: "Drop to Root" zone for promoting articles to root level
- Phase 10: Circular reference prevention (cannot drop on self or descendants)
- Phase 10: PATCH `/api/articles/{id}/parent` endpoint
- Phase 10: Stop propagation on all drag events to prevent bubbling
- Phase 10: Pointer-events disabled on inner elements during drag for consistent highlighting
- All Phase 10 features tested and working end-to-end

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
| 10 | Drag & Drop | 1 | ✅ **COMPLETE** | Tree reorganization |
| 11 | Icons & Polish | 1 | 📜 Next | Custom icons, final touches |
| 12 | Testing & Deploy | 2 | ⏳ Pending | E2E tests, optimization, production |

---

## Phases 0-8: Completed Foundation

**Status:** ✅ All Complete

### Phase 0: Infrastructure & Project Setup
- Azure Resource Group, SQL Database, Key Vault, Static Web App
- Local development environment with .NET 10
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
**Implementation Time:** ~8 hours (including troubleshooting)

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

**Response Structure:**
```json
{
  "query": "waterdeep",
  "titleMatches": [...],
  "bodyMatches": [...],
  "hashtagMatches": [...],
  "totalResults": 17
}
```

### Frontend Implementation

- Global Search Box in app header
- SearchResults page with grouped results
- SearchResultCard component with highlighting
- SearchApiService for API communication

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
**Implementation Time:** ~1 hour with Claude Opus 4.5

### Overview

Phase 9.5 refactors authentication across both backend (Azure Functions) and frontend (Blazor WASM) to use centralized patterns instead of per-function/per-service authentication calls.

### Backend Solution: Global Middleware

**AuthenticationMiddleware.cs:**
Implements `IFunctionsWorkerMiddleware` to handle JWT validation globally:
- Validates Auth0 JWT token on every HTTP request
- Skips validation for `[AllowAnonymous]` endpoints
- Stores authenticated user in `FunctionContext.Items["User"]`
- Returns 401 Unauthorized automatically for invalid/missing tokens

**FunctionContextExtensions.cs:**
Extension methods for easy user access:
```csharp
var user = context.GetRequiredUser();  // Throws if not authenticated
var user = context.GetUser();          // Returns null if not authenticated
```

### Frontend Solution: Centralized HttpClient

**AuthorizationMessageHandler.cs:**
`DelegatingHandler` that automatically attaches bearer tokens to all requests.

**Program.cs Registration:**
- Named client `"ChronicisApi"` with automatic auth
- All services use `IHttpClientFactory.CreateClient("ChronicisApi")`

### Success Criteria

1. ✅ Health endpoint works without authentication (`[AllowAnonymous]`)
2. ✅ Protected endpoints return 401 without token
3. ✅ Protected endpoints work with valid token
4. ✅ User accessible via `context.GetRequiredUser()` in all functions
5. ✅ All frontend services automatically include auth token
6. ✅ No redundant base classes or wrappers
7. ✅ Clean build with no warnings
8. ✅ End-to-end functionality verified

---

## Phase 10: Drag-and-Drop Reorganization

**Status:** ✅ **COMPLETE** (v2.0)

**Goal:** Allow dragging articles to reorganize hierarchy

**Completed:** December 2, 2025  
**Implementation Time:** ~2 hours with Claude Opus 4.5

### Overview

Phase 10 adds drag-and-drop functionality to the navigation tree, allowing users to reorganize their campaign knowledge structure by dragging articles to new parents or to root level.

### Backend Implementation

**New DTO:**
```csharp
public class ArticleMoveDto
{
    public int? NewParentId { get; set; }  // null = move to root
}
```

**New Service Method:**
```csharp
Task<(bool Success, string? ErrorMessage)> MoveArticleAsync(int articleId, int? newParentId, int userId);
```

**Circular Reference Detection:**
- Walks up from target parent to root
- If the article being moved appears in the ancestor chain, rejects the move
- Prevents data corruption from circular hierarchies

**New Endpoint:**
```
PATCH /api/articles/{id}/parent
Body: { "newParentId": int | null }
```

### Frontend Implementation

**HTML5 Drag API via Blazor:**
- `@ondragstart`, `@ondragover`, `@ondrop` events on tree nodes
- Stop propagation on all drag events to prevent bubbling to parent nodes
- `pointer-events: none` on inner elements during drag for consistent drop targeting

**"Drop to Root" Zone:**
- Appears at top of tree only when dragging
- Highlighted when hovering with valid drag source
- Accepts drops to promote articles to root level

**Visual Feedback:**
- Dragged article fades to 50% opacity
- Valid drop targets highlight with beige-gold glow and left border
- "Drop to Root" zone pulses when active
- Drag handle icon visible on hover

**Post-Drop Behavior:**
1. API call to move article
2. Tree refreshes from server
3. Moved article expanded and selected in new location
4. Toast notification confirms success

### Files Created

| File | Purpose |
|------|---------|
| `Functions/MoveArticle.cs` | PATCH endpoint for moving articles |

### Files Modified

| File | Changes |
|------|---------|
| `Shared/DTOs/ArticleDTOs.cs` | Added `ArticleMoveDto` class |
| `Services/ArticleService.cs` | Added `MoveArticleAsync` with circular reference check |
| `Services/IArticleApiService.cs` | Added `MoveArticleAsync` method |
| `Services/ArticleApiService.cs` | Implemented `MoveArticleAsync` with `PatchAsJsonAsync` |
| `Components/Articles/ArticleTreeView.razor` | Full drag-drop implementation |
| `wwwroot/css/chronicis-nav.css` | Drag-drop visual styles |

### Key Implementation Details

**Event Propagation:**
All drag events use `:stopPropagation="true"` to prevent parent nodes from capturing child drag operations:
```razor
@ondragstart:stopPropagation="true"
@ondragend:stopPropagation="true"
@ondragover:stopPropagation="true"
@ondragleave:stopPropagation="true"
@ondrop:stopPropagation="true"
```

**Consistent Drop Targeting:**
A `chronicis-nav-menu--dragging` class is added during drag, which applies `pointer-events: none` to inner elements, ensuring only the container div receives drag events.

### Success Criteria

1. ✅ Can drag article to new parent
2. ✅ Cannot create circular references
3. ✅ Tree updates immediately after move
4. ✅ Clear visual feedback during drag
5. ✅ Article remains selected in new location after move
6. ✅ "Drop to Root" zone works correctly
7. ✅ Child articles stay attached when parent moves
8. ✅ Consistent hover highlighting (no flicker)

---

## Phase 11: Custom Icons & Visual Enhancements

**Status:** 📜 Next Phase

**Goal:** Allow custom emoji icons and final polish

### Backend

- IconEmoji field already added in Phase 5
- Update endpoints to accept IconEmoji in updates (already done)

### Frontend

- EmojiPicker component
- Icon selection in inline editor (near title)
- Display icons in tree view (already done)
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

## Phase 12: Testing, Performance & Deployment

**Status:** ⏳ Pending

**Goal:** Ensure quality, optimize, deploy to production

### Testing Strategy

- Unit tests for Article CRUD
- Unit tests for hashtag parsing
- Unit tests for AI summary service
- Unit tests for search functionality
- Unit tests for authentication middleware
- Unit tests for article move/circular reference detection
- Integration tests for API endpoints
- Manual test plan execution
- Test inline editing edge cases
- Test drag-and-drop edge cases

### Performance Optimizations

- Add database indexes (Title, Hashtag.Name, Body for full-text)
- Query optimization with projections
- Frontend debouncing (already done for auto-save)
- Response compression
- Caching headers
- AI summary caching strategy

### Deployment Steps

- Validate Azure infrastructure
- Configure GitHub Actions
- Set environment variables
- Run database migrations on Azure SQL
- Deploy Azure OpenAI configuration to production
- Smoke test deployed app
- Set up Application Insights
- Configure monitoring and alerts

### Success Criteria

1. All tests passing
2. Performance meets targets
3. Successfully deployed to Azure
4. Monitoring configured
5. Inline editing works in production
6. AI summaries working in production
7. Search performance acceptable
8. Drag-and-drop works in production
9. Costs monitored and within budget

---

## Appendices

### A. Essential Commands

**Project Setup:**
```bash
dotnet new sln -n Chronicis
dotnet new blazorwasm -n Chronicis.Client
dotnet new func -n Chronicis.Api
dotnet sln add src/Chronicis.Client src/Chronicis.Api
```

**Development (PowerShell):**
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
```bash
az login
az group create --name rg-chronicis-dev --location eastus
az sql server create --name sql-chronicis-dev ...
az keyvault create --name kv-chronicis-dev ...
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
- **Dialog Open:** < 200ms
- **Drag-Drop Move:** < 500ms

### C. Project Structure

```
chronicis/
├── src/
│   ├── Chronicis.Client/           # Blazor WASM
│   │   ├── Components/
│   │   │   ├── Articles/
│   │   │   │   ├── ArticleDetail.razor
│   │   │   │   ├── ArticleTreeView.razor      # Updated Phase 10
│   │   │   │   ├── BacklinksPanel.razor
│   │   │   │   ├── AISummarySection.razor
│   │   │   │   └── SearchResultCard.razor
│   │   │   └── Hashtags/
│   │   │       └── HashtagLinkDialog.razor
│   │   ├── Services/
│   │   │   ├── ArticleApiService.cs           # Updated Phase 10
│   │   │   ├── IArticleApiService.cs          # Updated Phase 10
│   │   │   ├── AuthorizationMessageHandler.cs
│   │   │   ├── TreeStateService.cs
│   │   │   ├── QuoteService.cs
│   │   │   ├── HashtagApiService.cs
│   │   │   ├── AISummaryApiService.cs
│   │   │   └── SearchApiService.cs
│   │   ├── Pages/
│   │   │   ├── Home.razor (dashboard + routing)
│   │   │   └── Search.razor
│   │   └── wwwroot/
│   │       ├── css/
│   │       │   ├── chronicis-home.css
│   │       │   ├── chronicis-nav.css          # Updated Phase 10
│   │       │   ├── chronicis-hashtags.css
│   │       │   ├── chronicis-hashtag-tooltip.css
│   │       │   ├── chronicis-backlinks.css
│   │       │   ├── chronicis-ai-summary.css
│   │       │   ├── chronicis-search.css
│   │       │   └── tipTapStyles.css
│   │       └── js/
│   │           ├── tipTapIntegration.js
│   │           └── tipTapHashtagExtension.js
│   ├── Chronicis.Api/              # Azure Functions
│   │   ├── Functions/
│   │   │   ├── ArticleFunctions.cs
│   │   │   ├── ArticleSearchFunction.cs
│   │   │   ├── HashtagFunctions.cs
│   │   │   ├── BacklinkFunctions.cs
│   │   │   ├── AISummaryFunctions.cs
│   │   │   ├── CreateArticle.cs
│   │   │   ├── UpdateArticle.cs
│   │   │   ├── DeleteArticle.cs
│   │   │   ├── MoveArticle.cs                 # NEW Phase 10
│   │   │   └── HealthFunction.cs
│   │   ├── Infrastructure/
│   │   │   ├── AuthenticationMiddleware.cs
│   │   │   ├── AllowAnonymousAttribute.cs
│   │   │   ├── FunctionContextExtensions.cs
│   │   │   ├── Auth0Configuration.cs
│   │   │   └── Auth0AuthenticationHelper.cs
│   │   ├── Services/
│   │   │   ├── HashtagParser.cs
│   │   │   ├── HashtagSyncService.cs
│   │   │   ├── AISummaryService.cs
│   │   │   ├── ArticleService.cs              # Updated Phase 10
│   │   │   └── UserService.cs
│   │   └── Data/
│   │       └── Entities/
│   │           ├── Article.cs
│   │           ├── Hashtag.cs
│   │           ├── ArticleHashtag.cs
│   │           └── User.cs
│   └── Chronicis.Shared/           # DTOs
│       └── DTOs/
│           ├── ArticleDTOs.cs                 # Updated Phase 10
│           ├── HashtagDto.cs
│           ├── BacklinkDto.cs
│           ├── HashtagPreviewDto.cs
│           └── SummaryDtos.cs
├── tests/
├── docs/
└── Chronicis.sln
```

### D. Troubleshooting

**Drag events bubbling to parent nodes:**
- Check: All drag events have `:stopPropagation="true"`
- Solution: Add `@ondragstart:stopPropagation="true"` etc. to the container div

**Inconsistent drop target highlighting:**
- Check: `chronicis-nav-menu--dragging` class applied during drag
- Check: Inner elements have `pointer-events: none` during drag
- Solution: Add dragging class to parent and CSS rule for pointer-events

**Article doesn't move:**
- Check browser console for JavaScript errors
- Check API terminal for HTTP errors
- Verify the PATCH endpoint is registered

**Circular reference error:**
- This is correct behavior - cannot drop article onto itself or its descendants
- The `WouldCreateCircularReferenceAsync` method walks up the ancestor chain

**Authentication middleware not running:**
- Check: `builder.UseMiddleware<AuthenticationMiddleware>()` in Program.cs
- Verify: Middleware registered in `ConfigureFunctionsWorkerDefaults`

**401 Unauthorized on all endpoints:**
- Check: Token being sent in Authorization header
- Verify: Auth0 audience matches configuration
- Check: `[AllowAnonymous]` attribute on public endpoints

### E. Using This Plan

**Before Starting Phase 11:**
1. Review Phase 11 specification
2. Check that all Phase 10 features are working
3. Create new chat with Claude
4. Upload this plan + spec PDFs
5. Say: "I'm ready to start Phase 11 - Custom Icons & Visual Enhancements"

**During Each Phase:**
1. Create new chat with Claude
2. Upload this plan + spec PDFs
3. Say: "I'm ready to start Phase X"
4. Mention completed phases and any variations
5. Work through deliverables
6. Use GitHub Copilot for code
7. Return to Claude for architecture

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
- Debugging tricky issues
- Code review

**Use GitHub Copilot for:**
- Implementation details
- Boilerplate code
- Common patterns
- Test generation
- Refactoring
- Quick syntax help

**Model Selection:**
- **Opus 4.5:** Best for architectural changes touching many files. Completed auth refactoring in ~1 hour, drag-drop in ~2 hours.
- **Sonnet 4:** Good for focused implementation tasks. Lower cost, but may need more iteration for complex multi-file changes.

**Workflow:**
1. Plan with Claude
2. Implement with Copilot
3. Review with Claude
4. Iterate until complete

---

## Final Notes

**Remember:**
- This is a learning project - focus on the process
- AI accelerates but doesn't replace judgment
- Build phase by phase - don't skip ahead
- Test frequently, commit often
- Document your learnings
- Have fun! 🎉🐉

**Phase 10 Complete! ✅**
Drag-and-drop reorganization fully implemented:
- ✅ HTML5 drag API via Blazor events
- ✅ "Drop to Root" zone for promoting articles
- ✅ Circular reference prevention
- ✅ PATCH `/api/articles/{id}/parent` endpoint
- ✅ Visual feedback (opacity, highlighting, cursors)
- ✅ Stop propagation prevents event bubbling
- ✅ Consistent hover highlighting
- ✅ End-to-end functionality verified

**Current Progress:**
**10 of 12 phases complete** (~83% of project)
- Phases 0-10: ✅ Complete
- Phase 11: 📜 Ready to start (Icons & Polish)
- Phase 12: ⏳ Pending (Testing & Deploy)

**When Ready to Start Phase 11:**
Create a new chat, upload this plan and the spec PDFs, and say:
*"I'm ready to start Phase 11 of Chronicis implementation - Custom Icons & Visual Enhancements. Note: Phases 0-10 are complete including drag-and-drop reorganization. All working perfectly!"*

---

**Version History:**
- 2.0 (2025-12-02): Phase 10 COMPLETE - Drag-and-drop reorganization with event propagation fixes
- 1.9 (2025-12-01): Phase 9.5 COMPLETE - Auth architecture refactoring
- 1.8 (2025-11-28): Phase 9 COMPLETE - Full-text content search
- 1.7 (2025-11-27): Phase 8 COMPLETE - AI summaries with Azure OpenAI
- 1.6 (2025-11-27): Phase 7 COMPLETE - Interactive hashtags, backlinks, tooltips
- 1.5 (2025-11-26): Phase 6 COMPLETE - Full hashtag system
- 1.4 (2025-11-25): Phase 5 COMPLETE - Dashboard, routing, title save
- 1.3 (2025-11-25): Phase 5 complete implementation
- 1.2 (2025-11-24): Phase 4 complete (TipTap)
- 1.1 (2025-11-23): Updated for inline editing
- 1.0 (2025-11-18): Initial plan

**License:** Part of the Chronicis project. Modify as needed for your team.

---

*End of Implementation Plan*
