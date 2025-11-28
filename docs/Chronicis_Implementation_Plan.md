# Chronicis Implementation Plan - Complete Reference

**Version:** 1.8 | **Date:** November 28, 2025  
**Purpose:** Complete phase-by-phase implementation guide with detailed specifications

**CHANGES IN v1.8:**
- Phase 9: **COMPLETE** with full-text content search system
- Phase 9: Global search box in app header (searches title + body + hashtags)
- Phase 9: SearchResults page with grouped results (Title, Content, Hashtag matches)
- Phase 9: SearchResultCard component with highlighting and breadcrumbs
- Phase 9: SearchApiService for frontend API communication
- Phase 9: Enhanced ArticleSearchFunction with snippet extraction
- Phase 9: Context snippets with highlighted search terms
- Phase 9: Click-to-navigate from search results
- Phase 9: Resolved Visual Studio file inclusion issue (create through VS, not copy)
- Phase 9: Fixed component namespace resolution (moved to Components/Articles)
- All Phase 9 features tested and working end-to-end

---

## Quick Navigation

- [Project Context](#project-context) | [Phase Overview](#phase-overview)
- [Phase 0-8 Summary](#phases-0-8-summary) | [Phase 9](#phase-9) | [Phase 10](#phase-10) | [Phase 11](#phase-11) | [Phase 12](#phase-12)
- [Appendices](#appendices)

---

## Project Context

**What:** Web-based knowledge management for D&D campaigns  
**Stack:** Blazor WASM + Azure Functions + Azure SQL + MudBlazor  
**Timeline:** 16 weeks (12 phases)  
**Approach:** Local dev ? Test ? Deploy to Azure when stable

**Key Specs:**
- Design: `/mnt/project/Chronicis_Style_Guide.pdf`
- Platform: `/mnt/project/ChronicisPlatformSpec_md.pdf`
- Features: `/mnt/project/Chronicis_Feature_Specification.pdf`

**Editing Paradigm:** Inline editing like Obsidian (always-editable fields, auto-save, no modal dialogs)

---

## Phase Overview

| # | Phase | Weeks | Status | Deliverables |
|---|-------|-------|--------|--------------|
| 0 | Infrastructure & Setup | 1 | ? Complete | Azure resources, local environment, skeleton app |
| 1 | Data Model & Tree Nav | 2 | ? Complete | Article entity, hierarchy, tree view |
| 2 | CRUD Operations & Inline Editing | 1 | ? Complete | Create, edit, delete with inline editing |
| 3 | Search & Discovery | 1 | ? Complete | Title search, filtering, dedicated API |
| 4 | Markdown & Rich Content | 1 | ? Complete | TipTap WYSIWYG editor, rendering |
| 5 | Visual Design & Polish | 1 | ? Complete | Style guide, UX, dashboard, routing |
| 6 | Hashtag System | 1 | ? Complete | Parsing, visual styling, storage, API |
| 7 | Backlinks & Graph | 1 | ? Complete | Backlinks panel, tooltips, navigation, linking UI |
| 8 | AI Summaries | 2 | ? Complete | Azure OpenAI integration, summary generation, cost controls |
| 9 | Advanced Search | 1 | ? **COMPLETE** | Full-text content search, grouped results, global UI |
| 10 | Drag & Drop | 1 | ?? Next | Tree reorganization |
| 11 | Icons & Polish | 1 | ? Pending | Custom icons, final touches |
| 12 | Testing & Deploy | 2 | ? Pending | E2E tests, optimization, production |

---

<a name="phases-0-8-summary"></a>

## Phases 0-8: Completed Foundation

**Status:** ? All Complete

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
- Markdown ? HTML conversion

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
- JavaScript ? Blazor event communication
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

**Key Learnings from Phases 0-8:**
- TipTap extensions enable rich interactive features
- Configuration-driven AI prompts allow easy tuning
- Drawer-based UI better than fixed panels
- JavaScript ? Blazor communication works perfectly
- First-time implementation success from good planning

---

<a name="phase-9"></a>

## Phase 9: Advanced Search & Content Discovery

**Status:** ? **COMPLETE** (v1.8)

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

**Key Backend Features:**
- `ExtractSnippet()` - Creates context windows around search terms
- `BuildBreadcrumbs()` - Generates full ancestor paths
- `CreateSlug()` - URL-friendly article identifiers
- Case-insensitive substring matching with `EF.Functions.Like`

### Frontend Implementation

**1. Global Search Box (MainLayout.razor):**
- Located in app header (top-right)
- Search on Enter key or button click
- Navigates to `/search?q={query}`
- Styled to match Chronicis theme

**2. SearchResults Page:**
- Route: `@page "/search"`
- Displays grouped results by match type
- Loading state with spinner
- Empty state messages
- Result count display

**3. SearchResultCard Component:**
- Located: `Components/Articles/SearchResultCard.razor`
- Displays individual search results
- Match type badge (Title/Content/Hashtag)
- Highlighted search terms (yellow background)
- Breadcrumb paths showing article location
- Relative timestamps ("2h ago", "3d ago")
- Click to navigate to article

**4. SearchApiService:**
- Frontend API client for content search
- URL encoding for query parameters
- Error handling and logging

**5. DTOs:**
- `ArticleSearchResultDto` - Individual result with snippet, breadcrumbs, match type
- `GlobalSearchResultsDto` - Grouped results container

### Files Created/Modified (v1.8)

**Backend:**
- ? `Functions/ArticleSearchFunction.cs` - Enhanced with snippet extraction
- ? `DTOs/ArticleDto.cs` - Added search result DTOs

**Frontend:**
- ? `Services/SearchApiService.cs` - NEW
- ? `Pages/Search.razor` - NEW (renamed from SearchResults.razor)
- ? `Components/Articles/SearchResultCard.razor` - NEW
- ? `Layout/MainLayout.razor` - Added global search box
- ? `Program.cs` - Registered SearchApiService
- ? `wwwroot/css/chronicis-search.css` - NEW (optional, not required if using MudPaper)

### Key Implementation Learnings

**Visual Studio File Inclusion Issue:**
- **Problem:** Razor files copied from ZIP weren't recognized by routing
- **Cause:** Files not properly included in project build system
- **Solution:** Create files through Visual Studio ("Add > Razor Component") rather than copying
- **Lesson:** VS needs to register new components in .csproj properly

**Component Namespace Resolution:**
- **Problem:** `SearchResultCard` not found even with `@namespace` directive
- **Cause:** Component in `Components/Search/` folder not in expected namespace
- **Solution:** Moved to `Components/Articles/` where other components live
- **Lesson:** Keep related components in same namespace for easier discovery

**Styling with MudPaper:**
- **Problem:** MudContainer has beige background, low contrast with text
- **Solution:** Use `MudPaper` instead for white card background
- **Lesson:** MudPaper provides better contrast for content-heavy pages

**SearchResultCard Indentation:**
- **Problem:** TypeScript errors "Decorators not valid here"
- **Cause:** Excessive whitespace indentation on multi-line component attributes
- **Solution:** Put attributes on single line or use minimal indentation
- **Lesson:** Razor parser can be sensitive to whitespace in component syntax

### Comparison: Tree Search vs Content Search

| Feature | Tree Search (Phase 3) | Content Search (Phase 9) |
|---------|----------------------|--------------------------|
| **Endpoint** | `/api/articles/search/title` | `/api/articles/search` |
| **Searches** | Title only | Title + Body + Hashtags |
| **UI Location** | Left sidebar | App header (global) |
| **Results** | Filters tree | Dedicated results page |
| **Navigation** | Same page | Navigate to `/search` |
| **Use Case** | Quick tree navigation | Finding content anywhere |

### Success Criteria

1. ? Global search box appears in app header
2. ? Search on Enter or button click navigates to results page
3. ? Results grouped by match type (Title, Content, Hashtag)
4. ? Query terms highlighted in yellow
5. ? Each result shows title, snippet, breadcrumbs, timestamp
6. ? Clicking result navigates to article
7. ? Clicking result selects article in tree
8. ? Empty results show helpful message
9. ? Loading state displays during search
10. ? No console errors or warnings
11. ? Works with MudPaper for proper styling

### Performance Notes

**Database Optimization:**
- Current: Uses `LIKE` operator for substring matching
- Handles 1000+ articles adequately
- For larger datasets, consider full-text indexing:
  ```sql
  CREATE FULLTEXT INDEX ON Articles(Title, Body)
  ```

**Result Limiting:**
- 20 results per category (60 total max)
- Prevents overwhelming UI
- Future: Could add pagination

---

<a name="phase-10"></a>

## Phase 10: Drag-and-Drop Reorganization

**Status:** ?? Next Phase

**Goal:** Allow dragging articles to reorganize hierarchy

### Backend

- PATCH /api/articles/{id}/parent
- Update ParentId
- Validate no circular references
- Walk up tree to detect cycles

### Frontend

- Enable drag-and-drop on tree navigation
- Validate drop targets
- Prevent dropping on self/descendants
- Visual feedback during drag
- Toast notification on success
- Optional: Undo functionality

### Success Criteria

1. Can drag article to new parent
2. Cannot create circular references
3. Tree updates immediately
4. Clear visual feedback
5. Article remains open in inline editor after move

---

<a name="phase-11"></a>

## Phase 11: Custom Icons & Visual Enhancements

**Status:** ? Pending

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

<a name="phase-12"></a>

## Phase 12: Testing, Performance & Deployment

**Status:** ? Pending

**Goal:** Ensure quality, optimize, deploy to production

### Testing Strategy

- Unit tests for Article CRUD
- Unit tests for hashtag parsing
- Unit tests for AI summary service
- Unit tests for search functionality
- Integration tests for API endpoints
- Manual test plan execution
- Test inline editing edge cases
- Test AI summary generation with various scenarios
- Test search with large datasets

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
8. Costs monitored and within budget

---

<a name="appendices"></a>

## Appendices

### A. Essential Commands

**Project Setup:**
```bash
dotnet new sln -n Chronicis
dotnet new blazorwasm -n Chronicis.Client
dotnet new func -n Chronicis.Api
dotnet sln add src/Chronicis.Client src/Chronicis.Api
```

**Development:**
```bash
# Run Blazor client with hot reload
cd src/Chronicis.Client && dotnet watch run

# Run Azure Functions
cd src/Chronicis.Api && func start

# EF Migrations
cd src/Chronicis.Api
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

### C. Project Structure

```
chronicis/
??? src/
?   ??? Chronicis.Client/           # Blazor WASM
?   ?   ??? Components/
?   ?   ?   ??? Articles/
?   ?   ?   ?   ??? ArticleDetail.razor
?   ?   ?   ?   ??? ArticleTreeView.razor
?   ?   ?   ?   ??? BacklinksPanel.razor
?   ?   ?   ?   ??? AISummarySection.razor
?   ?   ?   ?   ??? SearchResultCard.razor
?   ?   ?   ??? Hashtags/
?   ?   ?       ??? HashtagLinkDialog.razor
?   ?   ??? Services/
?   ?   ?   ??? ArticleApiService.cs
?   ?   ?   ??? TreeStateService.cs
?   ?   ?   ??? QuoteService.cs
?   ?   ?   ??? HashtagApiService.cs
?   ?   ?   ??? AISummaryApiService.cs
?   ?   ?   ??? SearchApiService.cs
?   ?   ??? Pages/
?   ?   ?   ??? Home.razor (dashboard + routing)
?   ?   ?   ??? Search.razor
?   ?   ??? wwwroot/
?   ?       ??? css/
?   ?       ?   ??? chronicis-home.css
?   ?       ?   ??? chronicis-nav.css
?   ?       ?   ??? chronicis-hashtags.css
?   ?       ?   ??? chronicis-hashtag-tooltip.css
?   ?       ?   ??? chronicis-backlinks.css
?   ?       ?   ??? chronicis-ai-summary.css
?   ?       ?   ??? chronicis-search.css
?   ?       ?   ??? tipTapStyles.css
?   ?       ??? js/
?   ?           ??? tipTapIntegration.js
?   ?           ??? tipTapHashtagExtension.js
?   ??? Chronicis.Api/              # Azure Functions
?   ?   ??? Functions/
?   ?   ?   ??? ArticleSearchFunction.cs
?   ?   ?   ??? HashtagFunctions.cs
?   ?   ?   ??? BacklinkFunctions.cs
?   ?   ?   ??? AISummaryFunctions.cs
?   ?   ?   ??? UpdateArticle.cs
?   ?   ??? Services/
?   ?   ?   ??? HashtagParser.cs
?   ?   ?   ??? HashtagSyncService.cs
?   ?   ?   ??? AISummaryService.cs
?   ?   ??? Data/
?   ?       ??? Entities/
?   ?           ??? Article.cs
?   ?           ??? Hashtag.cs
?   ?           ??? ArticleHashtag.cs
?   ??? Chronicis.Shared/           # DTOs
?       ??? DTOs/
?           ??? ArticleDto.cs
?           ??? HashtagDto.cs
?           ??? BacklinkDto.cs
?           ??? HashtagPreviewDto.cs
?           ??? SummaryDtos.cs
??? tests/
??? docs/
??? Chronicis.sln
```

### D. Troubleshooting

**Navigation tree not showing expand arrows:**
- Check: API's `MapToDtoWithChildCount` sets ChildCount
- Verify: `Include(a => a.Children)` in GetChildrenAsync
- Solution: Use explicit DB count for ChildCount

**Articles not loading when clicked:**
- Check: Home.razor uses `TreeStateService.SelectedArticleId.HasValue`
- Verify: ArticleDetail subscribes to `TreeState.OnStateChanged`
- Solution: Update Home.razor to check SelectedArticleId

**Search page shows "Sorry, there's nothing at this address":**
- Check: File created through Visual Studio, not copied
- Verify: `@page "/search"` directive at top of file
- Solution: Delete file, recreate through VS "Add > Razor Component"

**SearchResultCard component not found:**
- Check: Component in correct namespace
- Verify: `@using` directive or full namespace path
- Solution: Move component to `Components/Articles/` folder

**Build warnings about decorators in Razor files:**
- Check: Multi-line component attributes with excessive indentation
- Solution: Put attributes on single line or minimal indentation

**Nullable reference warnings:**
- Check: Fields declared as nullable (`?`) but constructor assigns non-null
- Solution: Remove `?` to make field non-nullable

**MudBlazor v7 parameter warnings:**
- Check: Obsolete parameters like `Clickable` on `MudList`
- Solution: Remove obsolete parameters (v7 removes many legacy props)

**Cannot connect to SQL:**
- For Docker: `docker start sql-server`
- Check connection string
- Verify SQL Server is running

**CORS errors:**
- Add CORS policy in API Program.cs
- Allow origin `https://localhost:5001`

### E. Using This Plan

**Before Starting Phase 10:**
1. Review Phase 10 specification
2. Check that all Phase 9 features are working
3. Create new chat with Claude
4. Upload this plan + spec PDFs
5. Say: "I'm ready to start Phase 10 - Drag & Drop Reorganization"

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
- Have fun! ????

**Phase 9 Complete! ?**
All features implemented and working:
- ? Global search box in app header
- ? Full-text search across titles, bodies, hashtags
- ? Grouped results by match type
- ? Context snippets with highlighting
- ? Breadcrumb navigation
- ? Click to navigate to articles
- ? SearchResultCard component
- ? SearchApiService integration
- ? Proper styling with MudPaper
- ? All build warnings resolved

**Current Progress:**
**9 of 12 phases complete** (75% of project)
- Phases 0-9: ? Complete
- Phase 10: ?? Ready to start (Drag & Drop)
- Phases 11-12: ? Pending

**When Ready to Start Phase 10:**
Create a new chat, upload this plan and the spec PDFs, and say:
*"I'm ready to start Phase 10 of Chronicis implementation - Drag & Drop Reorganization. Note: Phases 0-9 are complete including full-text content search with global UI, grouped results, and click navigation. All working perfectly!"*

---

**Version History:**
- 1.8 (2025-11-28): Phase 9 COMPLETE - Full-text content search, global UI, grouped results, all working!
- 1.7 (2025-11-27): Phase 8 COMPLETE - AI summaries with Azure OpenAI, cost controls, full integration
- 1.6 (2025-11-27): Phase 7 COMPLETE - Interactive hashtags, backlinks, tooltips, linking UI
- 1.5 (2025-11-26): Phase 6 COMPLETE - Full hashtag system with parsing, storage, visual styling
- 1.4 (2025-11-25): Phase 5 COMPLETE - Dashboard, routing, title save, tree expansion
- 1.3 (2025-11-25): Phase 5 complete implementation with all fixes
- 1.2 (2025-11-24): Phase 4 complete rewrite using TipTap v3.11.0
- 1.1 (2025-11-23): Updated for inline editing paradigm
- 1.0 (2025-11-18): Initial comprehensive plan

**License:** Part of the Chronicis project. Modify as needed for your team.

---

*End of Implementation Plan*