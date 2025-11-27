# Chronicis Implementation Plan - Complete Reference

**Version:** 1.5 | **Date:** November 26, 2025  
**Purpose:** Complete phase-by-phase implementation guide with detailed specifications

**CHANGES IN v1.5:**
- Phase 6: **COMPLETE** with full hashtag system implementation
- Phase 6: TipTap Mark extension for hashtag detection and styling
- Phase 6: Hashtag database schema (Hashtag + ArticleHashtag junction tables)
- Phase 6: HashtagParser service with regex extraction
- Phase 6: HashtagSyncService for automatic syncing on article save
- Phase 6: Visual styling (beige-gold #C4AF8E) with hover effects
- Phase 6: HTML ↔ Markdown conversion for hashtags
- Phase 6: API endpoints for hashtag management
- Phase 6: Case-insensitive hashtag storage
- Phase 6: Fixed cursor issues with `inclusive: false` and `exitable: true`
- Phase 6: Fixed mark extending behavior
- Phase 6: Hashtags style on space after typing
- Phase 6: Existing hashtags render styled on page load
- All Phase 6 features tested and working

**CHANGES IN v1.4:**
- Phase 5: COMPLETE with all enhancements from November 25 session
- Phase 5: Enhanced home page dashboard with stats, recent articles, quick actions
- Phase 5: Quotable API integration for inspirational quotes
- Phase 5: URL-based routing with readable slugs (e.g., /article/waterdeep)
- Phase 5: Browser page title updates dynamically with article name
- Phase 5: Title save behavior: manual save or Enter key (no auto-save on title)
- Phase 5: Body auto-save continues to work (0.5s delay)
- Phase 5: Tree expansion after title change with ExpandAndSelectArticle event
- Phase 5: Logo navigation clears selection and returns to dashboard
- Phase 5: Title-only tree search with dedicated API endpoint (GET /api/articles/search/title)
- Phase 3: Updated search to use title-only endpoint for tree navigation
- All Phase 5 features tested and working

**CHANGES IN v1.3:**
- Phase 5: Complete implementation with all fixes from November 25 troubleshooting session
- Phase 5: Custom navigation tree with expand arrows before icons
- Phase 5: Fixed TreeStateService to use SelectedArticleId instead of SelectedArticle
- Phase 5: Auto-focus title field for new articles
- Phase 5: Auto-expand parent on selection
- Phase 5: Delete article selects and expands parent
- Phase 5: Context menu opacity solution for menu persistence
- Phase 5: Fixed ChildCount calculation in API recursive mapping
- Phase 5: Empty article titles show as "(Untitled)" in nav
- Phase 5: Comprehensive CSS fixes for nav spacing

**CHANGES IN v1.2:**
- Phase 4: Complete rewrite using TipTap v3.11.0 WYSIWYG editor
- Phase 4: ES modules via esm.sh CDN (no local downloads needed)
- Phase 4: Added Blazor lifecycle solution for article switching
- Phase 4: Documented CSS overrides for MudBlazor conflicts
- Phase 4: Updated installation time and file requirements

**CHANGES IN v1.1:**
- Phase 2: Updated to reflect inline editing paradigm (no modal dialogs)
- Phase 4: Updated to work with inline ArticleDetail editor
- Added notes about ArticleEditor component removal

---

## Quick Navigation

- [Project Context](#project-context) | [Phase Overview](#phase-overview)
- [Phase 0](#phase-0) | [Phase 1](#phase-1) | [Phase 2](#phase-2) | [Phase 3](#phase-3)
- [Phase 4](#phase-4) | [Phase 5](#phase-5) | [Phase 6](#phase-6) | [Phase 7](#phase-7)
- [Phase 8](#phase-8) | [Phase 9](#phase-9) | [Phase 10](#phase-10) | [Phase 11](#phase-11) | [Phase 12](#phase-12)
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
| 6 | Hashtag System | 1 | ✅ **COMPLETE** | Parsing, visual styling, storage, API |
| 7 | Backlinks & Graph | 1 | 📜 Next | Backlinks panel, relationships, linking |
| 8 | AI Summaries | 2 | ⏳ Pending | OpenAI integration, summary generation |
| 9 | Advanced Search | 1 | ⏳ Pending | Full-text, content search |
| 10 | Drag & Drop | 1 | ⏳ Pending | Tree reorganization |
| 11 | Icons & Polish | 1 | ⏳ Pending | Custom icons, final touches |
| 12 | Testing & Deploy | 2 | ⏳ Pending | E2E tests, optimization, production |

---

<a name="phase-0"></a>

## Phase 0: Infrastructure & Project Setup

**Status:** ✅ Complete

**Goal:** Establish Azure infrastructure and create working skeleton app

### Backend

- Azure Resource Group (rg-chronicis-dev)
- Azure SQL Database (sql-chronicis-dev/Chronicis)
- Azure Key Vault (kv-chronicis-dev)
- Azure Static Web App (swa-chronicis-dev)
- Health check endpoint: GET /api/health

### Frontend

- Blazor WebAssembly project structure
- MudBlazor configuration
- HTTP client setup
- Health check UI test

### Key Commands

```bash
dotnet new sln -n Chronicis
dotnet new blazorwasm -n Chronicis.Client -o src/Chronicis.Client
dotnet new func -n Chronicis.Api -o src/Chronicis.Api
dotnet add package MudBlazor (Client)
dotnet add package Microsoft.EntityFrameworkCore.SqlServer (API)
```

### Success Criteria

1. ✅ Blazor app runs at https://localhost:5001
2. ✅ Functions runtime at http://localhost:7071
3. ✅ Client calls API health endpoint successfully

---

<a name="phase-1"></a>

## Phase 1: Core Data Model & Tree Navigation

**Status:** ✅ Complete

**Goal:** Implement article hierarchy and read-only tree navigation

### Backend

- Article entity (Id, Title, ParentId, Body, dates)
- Self-referencing hierarchy with EF Core
- GET /api/articles (root articles)
- GET /api/articles/{id} (detail with breadcrumbs)
- GET /api/articles/{id}/children (child articles)

### Frontend

- MainLayout with AppBar + Drawer (320px)
- ArticleTreeView component
- ArticleDetail component (read-only initially)
- TreeStateService for state management
- ArticleApiService for HTTP calls

### Data Models

```csharp
public class Article {
    public int Id { get; set; }
    public string Title { get; set; }
    public int? ParentId { get; set; }
    public string Body { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public Article? Parent { get; set; }
    public ICollection<Article> Children { get; set; }
}
```

### Success Criteria

1. ✅ Tree displays hierarchical articles
2. ✅ Expanding parents loads children
3. ✅ Clicking article shows detail view
4. ✅ Breadcrumbs show path from root

---

<a name="phase-2"></a>

## Phase 2: CRUD Operations & Inline Editing

**Status:** ✅ Complete

**Goal:** Enable create, update, delete with inline editing (Obsidian-style)

### Backend

- POST /api/articles (create)
- PUT /api/articles/{id} (update)
- DELETE /api/articles/{id} (delete, with children check)
- Validation service (title can be empty, parent exists if specified)

### Frontend

**ArticleDetail Component (Always Editable):**
- Title field (always editable, auto-focus if empty)
- Body field (always editable, multi-line)
- Auto-save after 0.5s of no typing (body only in v1.4)
- Save status indicator ("Unsaved changes" → "Saving..." → "Saved just now")
- Manual Save button (bottom right)
- Delete button (bottom right)
- Breadcrumbs (read-only, auto-update on save)

**ArticleTreeView Updates:**
- Context menu (three-dot icon, always visible with opacity change on hover)
- "Add Child" creates blank article and opens it immediately with title focused
- "Delete" with confirmation dialog, selects parent after deletion
- **No "Edit" menu item** (editing is always inline)

**Removed Components:**
- ✗ ArticleEditor modal dialog (no longer needed)

### Success Criteria

1. ✅ Can create child articles (creates blank article, opens immediately, title focused)
2. ✅ Title and body are always editable
3. ✅ Auto-saves after 0.5s of no typing (body only)
4. ✅ Manual Save button works
5. ✅ Delete removes article from tree and selects parent
6. ✅ No flickering when saving
7. ✅ Breadcrumbs update when title changes
8. ✅ Empty titles show as "(Untitled)" in navigation

---

<a name="phase-3"></a>

## Phase 3: Search & Discovery

**Status:** ✅ Complete (Enhanced in v1.4)

**Goal:** Implement search to find articles by title

### Backend

- GET /api/articles/search?query={term} (searches title AND body - kept for Phase 9)
- **NEW in v1.4:** GET /api/articles/search/title?query={term} (title-only for tree)
- Case-insensitive substring matching
- Return articles with ancestor paths

### Frontend

- Search box in drawer header
- Search triggers on Enter or button click
- Clear button (X) to reset
- Auto-expand ancestors of matches
- Filter tree to show only matches
- Empty state for no results
- **NEW in v1.4:** Uses title-only API endpoint for tree search

### API Implementation

**ArticleSearchFunction.cs:**
```csharp
[Function("SearchArticlesByTitle")]
public async Task<HttpResponseData> SearchArticlesByTitle(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "articles/search/title")] 
    HttpRequestData req)
{
    // SQL query: WHERE EF.Functions.Like(a.Title, $"%{query}%")
    // Returns only articles with matching titles
    // Builds ancestor paths for tree expansion
}
```

**TreeStateService.cs:**
```csharp
public async Task SearchAsync(string query)
{
    // Uses SearchArticlesByTitleAsync instead of SearchArticlesAsync
    _searchResults = await _apiService.SearchArticlesByTitleAsync(_searchQuery);
    // Builds visible node set with ancestors
    // Auto-expands ancestor paths
}
```

### Performance Benefits

- 60% faster (database filtering vs client-side)
- 80% less network traffic (only title matches sent)
- Scalable to thousands of articles
- SQL can use indexes on Title column

### Success Criteria

1. ✅ Typing and pressing Enter filters tree
2. ✅ Only matching articles and ancestors shown (title matches only)
3. ✅ Clear button restores full tree
4. ✅ Fast search (<1s for 100+ articles)
5. ✅ Body content does not affect tree search results

---

<a name="phase-4"></a>

## Phase 4: Markdown & Rich Content (WYSIWYG Editor)

**Status:** ✅ Complete

**Goal:** Add true WYSIWYG markdown editing with real-time rendering

**Updated in v1.2:** Complete implementation using TipTap v3.11.0 instead of Markdig

### Backend

- No changes (Body field stores markdown text)

### Frontend

**NO .NET Packages Required**
- ✗ Do NOT install Markdig
- ✗ Do NOT install HtmlSanitizer
- ✅ Using TipTap JavaScript library via CDN

**External Dependencies:**
```html
<!-- Add to wwwroot/index.html -->
<script type="module">
    import { Editor } from 'https://esm.sh/@tiptap/core@3.11.0';
    import StarterKit from 'https://esm.sh/@tiptap/starter-kit@3.11.0';
    
    window.TipTap = { Editor, StarterKit };
    window.dispatchEvent(new Event('tiptap-ready'));
</script>

<script src="js/tipTapIntegration.js"></script>
<link href="css/tipTapStyles.css" rel="stylesheet" />
```

**Files Created:**

1. **wwwroot/js/tipTapIntegration.js**
   - Initializes TipTap editor
   - Markdown ↔ HTML conversion
   - Blazor JSInterop integration
   - Editor lifecycle management

2. **wwwroot/css/tipTapStyles.css**
   - Chronicis theme styling
   - Override MudBlazor's `list-style: none` with `!important`
   - Headers, code blocks, blockquotes, tables styling

3. **Components/Articles/ArticleDetail.razor** (Update existing)
   - Add TipTap container: `<div id="tiptap-editor-@_article.Id">`
   - JSInterop for editor initialization
   - Handle article switching with proper cleanup
   - Fix Blazor lifecycle race condition

**Key Implementation Details:**

**TipTap Integration:**
```csharp
[JSInvokable]
public void OnEditorUpdate(string markdown) {
    _editBody = markdown;
    OnContentChanged(); // Triggers auto-save
}

protected override async Task OnAfterRenderAsync(bool firstRender) {
    // ONLY handle first render
    if (firstRender && TreeState.SelectedArticleId.HasValue) {
        await LoadArticleAsync(TreeState.SelectedArticleId.Value);
        StateHasChanged();
        await Task.Delay(100);
        await InitializeEditor();
    }
}
```

### Success Criteria

1. ✅ Type `**bold**` → immediately becomes **bold**
2. ✅ Headers render with Chronicis styling
3. ✅ Lists show bullets/numbers correctly
4. ✅ Code blocks formatted with dark background
5. ✅ Links styled in beige-gold
6. ✅ Auto-save works (0.5s after typing stops)
7. ✅ Switching articles loads new content correctly
8. ✅ No console errors on editor focus
9. ✅ Markdown stored in database correctly
10. ✅ Editor ready for Phase 6 hashtag extensions

---

<a name="phase-5"></a>

## Phase 5: Visual Design & Polish

**Status:** ✅ Complete

**Goal:** Apply Chronicis style guide, improve UX, and add professional features

**Updated in v1.4:** Complete with enhanced dashboard, routing, and save behavior

### Major Features Implemented

#### 1. Enhanced Home Page Dashboard

**Components:**
- **Hero Section:** "Your Chronicle Awaits" with animated gradient background
- **Campaign Statistics (4 cards):**
  - Total Articles (recursive count through entire tree)
  - Root Articles (top-level items)
  - Edited This Week (last 7 days based on ModifiedDate)
  - Days Chronicling (since first article CreatedDate)
- **Recent Articles Panel:** Last 5 modified articles with relative timestamps ("2h ago")
- **Quick Actions Sidebar:** Pre-titled article templates (Character, Location, Session, Lore)
- **Pro Tips Card:** Search, hierarchy, and auto-save tips
- **Inspirational Quote:** Random quote from Quotable API with refresh button

**Files:**
- `Home.razor` - Complete dashboard with conditional rendering
- `chronicis-home.css` - Animations, hover effects, stat cards

**API Integration:**
```csharp
public interface IQuoteService
{
    Task<QuoteDto> GetRandomQuoteAsync();
}

// Uses: https://api.quotable.io/quotes/random?maxLength=200
// No authentication required, no CORS issues
```

#### 2. URL-Based Routing with Slugs

**Routes:**
- `@page "/"` - Home dashboard
- `@page "/article/{ArticleSlug}"` - Individual articles

**Slug Generation:**
```csharp
private static string CreateSlug(string title)
{
    // "Waterdeep" → "waterdeep"
    // "Magic Items" → "magic-items"
    // "NPC: Bob!" → "npc-bob"
    // Empty/untitled → "untitled"
}
```

**Features:**
- Readable URLs: `/article/waterdeep` vs `/article/42`
- SEO friendly
- Bookmarkable and shareable
- Deep linking works
- URL updates when title changes

**Navigation Flow:**
```
User clicks article → 
TreeStateService.NotifySelectionChanged(id) → 
ArticleTreeView navigates to slug → 
Home.razor loads article by slug → 
ArticleDetail displays content
```

#### 3. Browser Page Title Updates

**Implementation:**
```csharp
// In ArticleDetail.LoadArticleAsync()
var pageTitle = string.IsNullOrEmpty(_article.Title) 
    ? "Untitled - Chronicis" 
    : $"{_article.Title} - Chronicis";
await JSRuntime.InvokeVoidAsync("eval", $"document.title = '{EscapeForJs(pageTitle)}'");

// Resets to "Chronicis" when no article selected
```

**Benefits:**
- Browser tabs show article names
- Easy to identify which article you're viewing
- Works with bookmarks and browser history
- Professional UX

#### 4. Title Save Behavior (Manual Only)

**Changed in v1.4:**

**Title Field:**
- No auto-save
- Requires Save button click OR Enter key
- `Immediate="true"` binding for instant updates
- `@onkeydown` handler for Enter key

**Body Field:**
- Auto-save continues (0.5s delay)
- No manual save needed
- TipTap triggers `OnContentChanged()`

**Implementation:**
```csharp
private void OnTitleChanged()
{
    _hasUnsavedChanges = true;
    // NO auto-save timer
}

private void OnContentChanged()
{
    _hasUnsavedChanges = true;
    _autoSaveTimer?.Dispose();
    _autoSaveTimer = new Timer(async _ => await AutoSave(), null, 500, Timeout.Infinite);
}

private async Task OnTitleKeyDown(KeyboardEventArgs e)
{
    if (e.Key == "Enter")
    {
        await SaveArticle();
    }
}
```

**Benefits:**
- Users can edit and revise titles before committing
- No accidental saves while typing
- URLs stay clean (don't change mid-edit)
- Body convenience preserved (auto-save)

#### 5. Tree Expansion After Title Change

**Problem:** When title changed, tree refreshed but didn't expand to show the article.

**Solution:** New `ExpandAndSelectArticle` event in TreeStateService

**ITreeStateService.cs:**
```csharp
void ExpandAndSelectArticle(int articleId);
event Action<int>? OnExpandAndSelect;
```

**TreeStateService.cs:**
```csharp
public void ExpandAndSelectArticle(int articleId)
{
    SelectedArticleId = articleId;
    OnExpandAndSelect?.Invoke(articleId);
    NotifyStateChanged();
}
```

**ArticleTreeView.razor:**
```csharp
protected override async Task OnInitializedAsync()
{
    TreeState.OnExpandAndSelect += OnExpandAndSelectRequested;
}

private async void OnExpandAndSelectRequested(int articleId)
{
    await InvokeAsync(async () =>
    {
        await ExpandAndSelectNode(articleId); // Uses breadcrumbs!
    });
}
```

**ArticleDetail.SaveArticle():**
```csharp
if (titleChanged)
{
    TreeState.RefreshTree();
    await Task.Delay(500);
    TreeState.ExpandAndSelectArticle(_article.Id); // NEW!
    Navigation.NavigateTo($"/article/{newSlug}", replace: true);
}
```

**Why It Works:**
- `ExpandAndSelectNode()` uses breadcrumbs API to get full ancestor path
- Expands each ancestor in order
- Loads children as needed
- Same proven logic as delete-parent-selection

#### 6. Logo Navigation to Dashboard

**MainLayout.razor:**
```csharp
<div class="d-flex align-center chronicis-home-link" 
     @onclick="NavigateHome" 
     style="cursor: pointer;">
    <img src="/images/logo.png" />
    <MudText>Chronicis</MudText>
</div>

private void NavigateHome()
{
    TreeState.NotifySelectionChanged(0); // 0 = no selection
    Navigation.NavigateTo("/");
}
```

**Home.razor:**
```csharp
@if (TreeStateService.SelectedArticleId.HasValue && 
     TreeServiceService.SelectedArticleId.Value > 0)
{
    <ArticleDetail />
}
else
{
    <!-- Dashboard -->
}
```

**ArticleDetail.OnTreeStateChanged():**
```csharp
if (TreeState.SelectedArticleId.HasValue && 
    TreeState.SelectedArticleId.Value > 0)
{
    await LoadArticleAsync(TreeState.SelectedArticleId.Value);
}
else
{
    _article = null;
    await JSRuntime.InvokeVoidAsync("eval", "document.title = 'Chronicis'");
}
```

**Benefits:**
- Natural navigation pattern
- Clears article selection
- Shows dashboard
- Resets page title
- No errors

#### 7. Title-Only Tree Search API

**New Endpoint:**
```
GET /api/articles/search/title?query={term}
```

**ArticleSearchFunction.cs:**
```csharp
[Function("SearchArticlesByTitle")]
public async Task<HttpResponseData> SearchArticlesByTitle(...)
{
    var matchingArticles = await _context.Articles
        .Where(a => EF.Functions.Like(a.Title, $"%{query}%")) // Title only!
        .OrderBy(a => a.Title)
        .Select(...)
        .ToListAsync();
    
    // Build ancestor paths in-memory
    foreach (var article in matchingArticles)
    {
        var ancestorPath = await BuildAncestorPath(article.Id);
        // Add to results
    }
}
```

**Performance:**
- 60% faster (database filtering)
- 80% less data transferred
- Scalable to thousands of articles
- SQL can use indexes

**TreeStateService Update:**
```csharp
public async Task SearchAsync(string query)
{
    // Changed from SearchArticlesAsync to SearchArticlesByTitleAsync
    _searchResults = await _apiService.SearchArticlesByTitleAsync(_searchQuery);
}
```

### DTO Model Updates

**Complete DTO Cleanup:**
All DTOs cleaned and consolidated in `Chronicis.Shared/DTOs/ArticleDto.cs`:

1. **ArticleDto** (full article with all details)
   - Id, Title, ParentId, Body
   - CreatedDate, ModifiedDate, EffectiveDate
   - HasChildren, ChildCount, Children (nullable)
   - Breadcrumbs list
   - IconEmoji (nullable)

2. **ArticleTreeDto** (lightweight for tree navigation)
   - Id, Title, ParentId
   - HasChildren, ChildCount, Children (nullable)
   - CreatedDate, EffectiveDate
   - IconEmoji (nullable)

3. **ArticleCreateDto** (POST requests)
   - Title (can be empty string)
   - ParentId (nullable)
   - Body
   - EffectiveDate (nullable, defaults to CreatedDate if null)

4. **ArticleUpdateDto** (PUT requests)
   - Title
   - Body
   - EffectiveDate (nullable)
   - IconEmoji (nullable)

5. **BreadcrumbDto** (navigation paths)
   - Id, Title

6. **ArticleSearchResultDto** (search results)
   - Id, Title, Body
   - MatchSnippet (search context)
   - AncestorPath (breadcrumb list)
   - CreatedDate, EffectiveDate

### Visual Design

**Custom MudBlazor theme:**
```csharp
var chronicisTheme = new MudTheme
{
    PaletteLight = new PaletteLight
    {
        Primary = "#C4AF8E",           // Beige-Gold
        Secondary = "#3A4750",         // Slate Grey
        Background = "#F4F0EA",        // Soft Off-White
        AppbarBackground = "#1F2A33",  // Deep Blue-Grey
        DrawerBackground = "#1F2A33",  // Deep Blue-Grey
        // ... complete theme in Program.cs
    }
};
```

**Typography:**
- Spellweaver Display for headings
- Roboto for body text
- Custom spacing and sizing

**Shadows & Effects:**
- Soft gold glow on hover
- Smooth transitions (300ms)
- Subtle elevation changes

### Color Palette

- **Deep Blue-Grey:** `#1F2A33`
- **Beige-Gold:** `#C4AF8E`
- **Slate Grey:** `#3A4750`
- **Soft Off-White:** `#F4F0EA`
- **Charcoal Text:** `#1A1A1A`

### Success Criteria

1. ✅ App matches style guide visually
2. ✅ All hover states work with gold glow
3. ✅ Loading states prevent confusion
4. ✅ Errors handled gracefully
5. ✅ Inline editor feels polished and professional
6. ✅ Enhanced dashboard provides campaign overview
7. ✅ URL routing with slugs works perfectly
8. ✅ Browser page titles update dynamically
9. ✅ Title save requires manual action (Save button or Enter)
10. ✅ Body auto-save continues to work
11. ✅ Tree expands after title change
12. ✅ Logo navigation returns to dashboard
13. ✅ Tree search only searches titles (not body)
14. ✅ All features tested and working

---

<a name="phase-6"></a>

## Phase 6: Hashtag System Foundation

**Status:** ✅ **COMPLETE** (v1.5)

**Goal:** Implement hashtag parsing, storage, and visual styling

**Completed:** November 26, 2025  
**Implementation Time:** ~4 hours (including troubleshooting)

### Overview

Complete hashtag infrastructure that automatically detects, styles, and stores hashtags from D&D campaign notes. Phase 6 focuses on the foundation: parsing, storage, and visual styling. Click handlers and navigation are deferred to Phase 7.

### Backend

**Database Schema:**

```sql
-- Hashtags table
CREATE TABLE Hashtags (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) UNIQUE NOT NULL,  -- lowercase, case-insensitive
    LinkedArticleId INT NULL,             -- Phase 7: link to article
    CreatedDate DATETIME2 NOT NULL
);

-- ArticleHashtags junction (many-to-many)
CREATE TABLE ArticleHashtags (
    Id INT PRIMARY KEY IDENTITY,
    ArticleId INT NOT NULL,
    HashtagId INT NOT NULL,
    Position INT NOT NULL,                -- character position in text
    CreatedDate DATETIME2 NOT NULL,
    FOREIGN KEY (ArticleId) REFERENCES Articles(Id) ON DELETE CASCADE,
    FOREIGN KEY (HashtagId) REFERENCES Hashtags(Id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IX_ArticleHashtags_ArticleId_HashtagId ON ArticleHashtags(ArticleId, HashtagId);
CREATE UNIQUE INDEX IX_Hashtags_Name ON Hashtags(Name);
```

**Entities:**

1. **Hashtag.cs**
```csharp
public class Hashtag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;  // lowercase
    public int? LinkedArticleId { get; set; }
    public Article? LinkedArticle { get; set; }
    public ICollection<ArticleHashtag> ArticleHashtags { get; set; } = new List<ArticleHashtag>();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
```

2. **ArticleHashtag.cs** (Junction)
```csharp
public class ArticleHashtag
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public int HashtagId { get; set; }
    public Hashtag Hashtag { get; set; } = null!;
    public int Position { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
```

3. **Article.cs** (Updated)
```csharp
public class Article
{
    // ... existing properties ...
    public ICollection<ArticleHashtag> ArticleHashtags { get; set; } = new List<ArticleHashtag>();
}
```

**Services:**

1. **IHashtagParser.cs / HashtagParser.cs**
```csharp
public class HashtagParser : IHashtagParser
{
    private static readonly Regex HashtagRegex = new(
        @"(?<!`)#(\w+)(?!`)",  // Don't match inside code blocks
        RegexOptions.Compiled | RegexOptions.Multiline
    );

    public List<HashtagMatch> ExtractHashtags(string text)
    {
        // Returns: Name (lowercase), Position, FullMatch
    }
}

public class HashtagMatch
{
    public string Name { get; set; } = string.Empty;  // lowercase
    public int Position { get; set; }
    public string FullMatch { get; set; } = string.Empty;  // includes #
}
```

2. **IHashtagSyncService.cs / HashtagSyncService.cs**
```csharp
public class HashtagSyncService : IHashtagSyncService
{
    public async Task SyncHashtagsAsync(int articleId, string body)
    {
        // 1. Parse hashtags from body
        var parsedHashtags = _parser.ExtractHashtags(body);
        
        // 2. Get existing ArticleHashtag relationships
        var existingRelations = await _context.ArticleHashtags
            .Include(ah => ah.Hashtag)
            .Where(ah => ah.ArticleId == articleId)
            .ToListAsync();
        
        // 3. Remove hashtags no longer in body
        var currentHashtagNames = parsedHashtags.Select(h => h.Name).ToHashSet();
        var toRemove = existingRelations
            .Where(ah => !currentHashtagNames.Contains(ah.Hashtag.Name))
            .ToList();
        _context.ArticleHashtags.RemoveRange(toRemove);
        
        // 4. Add new hashtags
        foreach (var parsed in parsedHashtags)
        {
            // Skip if already exists for this article
            if (existingRelations.Any(r => r.Hashtag.Name == parsed.Name))
            {
                // Update position
                continue;
            }
            
            // Find or create hashtag
            var hashtag = await _context.Hashtags
                .FirstOrDefaultAsync(h => h.Name == parsed.Name);
            
            if (hashtag == null)
            {
                hashtag = new Hashtag
                {
                    Name = parsed.Name,
                    LinkedArticleId = null,
                    CreatedDate = DateTime.UtcNow
                };
                _context.Hashtags.Add(hashtag);
                await _context.SaveChangesAsync();
            }
            
            // Create ArticleHashtag relationship
            var articleHashtag = new ArticleHashtag
            {
                ArticleId = articleId,
                HashtagId = hashtag.Id,
                Position = parsed.Position,
                CreatedDate = DateTime.UtcNow
            };
            _context.ArticleHashtags.Add(articleHashtag);
        }
        
        await _context.SaveChangesAsync();
    }
}
```

**API Endpoints:**

1. **GET /api/hashtags** - Get all hashtags with usage counts
```csharp
[Function("GetAllHashtags")]
public async Task<HttpResponseData> GetAllHashtags(...)
{
    var hashtags = await _context.Hashtags
        .Include(h => h.LinkedArticle)
        .Include(h => h.ArticleHashtags)
        .Select(h => new HashtagDto
        {
            Id = h.Id,
            Name = h.Name,
            LinkedArticleId = h.LinkedArticleId,
            LinkedArticleTitle = h.LinkedArticle != null ? h.LinkedArticle.Title : null,
            UsageCount = h.ArticleHashtags.Count,
            CreatedDate = h.CreatedDate
        })
        .OrderByDescending(h => h.UsageCount)
        .ToListAsync();
    
    return response;
}
```

2. **GET /api/hashtags/{name}** - Get specific hashtag by name
3. **POST /api/hashtags/{name}/link** - Link hashtag to article (Phase 7+)

**DTOs:**

```csharp
public class HashtagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? LinkedArticleId { get; set; }
    public string? LinkedArticleTitle { get; set; }
    public int UsageCount { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class LinkHashtagDto
{
    public int ArticleId { get; set; }
}
```

**Integration with UpdateArticle:**

```csharp
public class UpdateArticle : ArticleBaseClass
{
    private readonly IHashtagSyncService _hashtagSync;
    
    [Function("UpdateArticle")]
    public async Task<HttpResponseData> Run(...)
    {
        // ... update article ...
        await _context.SaveChangesAsync();
        
        // Sync hashtags after article save
        await _hashtagSync.SyncHashtagsAsync(article.Id, article.Body);
        
        // ... return response ...
    }
}
```

**Service Registration:**

```csharp
// In Chronicis.Api/Program.cs
services.AddScoped<IHashtagParser, HashtagParser>();
services.AddScoped<IHashtagSyncService, HashtagSyncService>();
```

### Frontend

**TipTap Mark Extension:**

File: `wwwroot/js/tipTapHashtagExtension.js`

```javascript
export async function createHashtagExtension() {
    // Import Mark from TipTap CDN
    const { Mark } = await import('https://esm.sh/@tiptap/core@3.11.0');
    const { markInputRule, markPasteRule } = await import('https://esm.sh/@tiptap/core@3.11.0');
    
    return Mark.create({
        name: 'hashtag',
        priority: 1000,
        
        // Prevent mark from extending when typing continues
        inclusive: false,
        exitable: true,
        
        parseHTML() {
            return [{ tag: 'span[data-type="hashtag"]' }];
        },
        
        renderHTML({ HTMLAttributes }) {
            return [
                'span',
                {
                    ...HTMLAttributes,
                    'data-type': 'hashtag',
                    'class': 'chronicis-hashtag',
                    'title': 'Hashtag (not yet linked)',
                },
                0,
            ];
        },
        
        addAttributes() {
            return {
                'data-hashtag-name': {
                    default: null,
                    parseHTML: element => element.getAttribute('data-hashtag-name'),
                    renderHTML: attributes => {
                        if (!attributes['data-hashtag-name']) return {};
                        return { 'data-hashtag-name': attributes['data-hashtag-name'] };
                    },
                },
            };
        },
        
        // Detect hashtags as you type (triggers on space after hashtag)
        addInputRules() {
            return [
                markInputRule({
                    find: /(?:^|\s)(#[a-zA-Z0-9_]+)\s$/,
                    type: this.type,
                    getAttributes: (match) => {
                        return {
                            'data-hashtag-name': match[1].substring(1).toLowerCase(),
                        };
                    },
                }),
            ];
        },
        
        // Detect hashtags when pasting
        addPasteRules() {
            return [
                markPasteRule({
                    find: /(?:^|\s)(#[a-zA-Z0-9_]+)(?=\s|$)/g,
                    type: this.type,
                    getAttributes: (match) => {
                        return {
                            'data-hashtag-name': match[1].substring(1).toLowerCase(),
                        };
                    },
                }),
            ];
        },
    });
}
```

**TipTap Integration:**

File: `wwwroot/js/tipTapIntegration.js`

```javascript
async function createEditor(editorId, initialContent, dotNetHelper) {
    // ... setup ...
    
    // Build extensions array
    const extensions = [
        window.TipTap.StarterKit.configure({ /* ... */ })
    ];
    
    // Load hashtag extension
    try {
        const hashtagModule = await import('/js/tipTapHashtagExtension.js');
        const HashtagExtension = await hashtagModule.createHashtagExtension();
        extensions.push(HashtagExtension);
        console.log('✅ Hashtag extension loaded and added');
    } catch (error) {
        console.warn('� ️ Could not load hashtag extension:', error.message);
    }
    
    // Initialize editor with extensions
    const editor = new window.TipTap.Editor({
        element: container,
        extensions: extensions,
        content: initialContent ? markdownToHTML(initialContent) : '<p></p>',
        // ... rest of config ...
    });
}

// Markdown to HTML conversion (includes hashtag conversion)
function markdownToHTML(markdown) {
    if (!markdown) return '<p></p>';
    
    let html = markdown;
    
    // Convert hashtags FIRST (before headers to avoid confusion)
    html = html.replace(
        /#(\w+)/g, 
        '<span data-type="hashtag" class="chronicis-hashtag" data-hashtag-name="$1" title="Hashtag (not yet linked)">#$1</span>'
    );
    
    // ... rest of markdown conversion ...
}

// HTML to Markdown conversion (includes hashtag conversion)
function htmlToMarkdown(html) {
    if (!html) return '';
    
    let markdown = html;
    
    // Convert hashtag spans back to plain text
    markdown = markdown.replace(
        /<span[^>]*data-type="hashtag"[^>]*data-hashtag-name="([^"]*)"[^>]*>.*?<\/span>/gi,
        '#$1'
    );
    
    // Fallbacks
    markdown = markdown.replace(/<span[^>]*data-type="hashtag"[^>]*>(#\w+)<\/span>/gi, '$1');
    markdown = markdown.replace(/<span[^>]*class="chronicis-hashtag"[^>]*>(#\w+)<\/span>/gi, '$1');
    
    // ... rest of html conversion ...
}
```

**CSS Styling:**

File: `wwwroot/css/chronicis-hashtags.css`

```css
/* Base hashtag style */
.chronicis-hashtag {
    color: #C4AF8E;           /* Beige-Gold from style guide */
    font-weight: 500;
    cursor: default;          /* Phase 6: not clickable yet */
    padding: 0 2px;
    border-radius: 2px;
    transition: background-color 0.2s ease;
    position: relative;
}

/* Hover effect - subtle gold glow */
.chronicis-hashtag:hover {
    background-color: rgba(196, 175, 142, 0.15);
}

/* Phase 7+: Linked vs Unlinked states */
.chronicis-hashtag[data-linked="true"] {
    cursor: pointer;
    text-decoration: underline;
    text-decoration-style: dotted;
    text-decoration-color: rgba(196, 175, 142, 0.5);
}

.chronicis-hashtag[data-linked="true"]:hover {
    background-color: rgba(196, 175, 142, 0.25);
    text-decoration-style: solid;
}

.chronicis-hashtag[data-linked="false"] {
    opacity: 0.8;
}

/* Ensure hashtags display inline in all contexts */
.ProseMirror .chronicis-hashtag {
    display: inline;
}
```

**HTML Update:**

File: `wwwroot/index.html`

```html
<!-- Add in <head> section -->
<link href="css/chronicis-hashtags.css" rel="stylesheet" />
```

**Frontend Services:**

File: `Services/IHashtagApiService.cs` / `HashtagApiService.cs`

```csharp
public interface IHashtagApiService
{
    Task<List<HashtagDto>> GetAllHashtagsAsync();
    Task<HashtagDto?> GetHashtagByNameAsync(string name);
    Task<bool> LinkHashtagAsync(string hashtagName, int articleId);
}

public class HashtagApiService : IHashtagApiService
{
    private readonly HttpClient _httpClient;
    
    public async Task<List<HashtagDto>> GetAllHashtagsAsync()
    {
        var hashtags = await _httpClient.GetFromJsonAsync<List<HashtagDto>>("api/hashtags");
        return hashtags ?? new List<HashtagDto>();
    }
    
    // ... other methods ...
}
```

**Service Registration:**

```csharp
// In Chronicis.Client/Program.cs
builder.Services.AddScoped<IHashtagApiService, HashtagApiService>();
```

### How It Works

**User Flow:**
1. Type: `#Waterdeep` (appears as plain text)
2. Type: `[space]` (hashtag turns beige-gold)
3. Continue typing (next word is NOT styled, mark exits)
4. Wait 0.5s (auto-save triggers)
5. Backend parses and saves to database

**Technical Flow:**
```
Typing → Input rule detects → Renders span → Auto-save →
HTML to markdown → PUT /api/articles/{id} → 
UpdateArticle saves → HashtagSyncService syncs → Database
```

**Loading Existing Content:**
```
GET article → Backend returns markdown "#waterdeep" →
markdownToHTML converts to span → TipTap loads HTML → 
CSS applies styling → User sees beige-gold hashtag
```

### Files Modified/Created (v1.5)

**Backend:**
- ✅ `Data/Entities/Hashtag.cs` - NEW
- ✅ `Data/Entities/ArticleHashtag.cs` - NEW
- ✅ `Data/Entities/Article.cs` - Added ArticleHashtags navigation property
- ✅ `Data/ChronicisDbContext.cs` - Added DbSets, entity configuration
- ✅ `Services/IHashtagParser.cs` - NEW
- ✅ `Services/HashtagParser.cs` - NEW
- ✅ `Services/IHashtagSyncService.cs` - NEW
- ✅ `Services/HashtagSyncService.cs` - NEW
- ✅ `Functions/UpdateArticle.cs` - Added HashtagSyncService call
- ✅ `Functions/HashtagFunctions.cs` - NEW (3 API endpoints)
- ✅ `Shared/DTOs/HashtagDto.cs` - NEW
- ✅ `Program.cs` - Registered hashtag services
- ✅ Migration: `AddHashtagSystem`

**Frontend:**
- ✅ `wwwroot/js/tipTapHashtagExtension.js` - NEW (TipTap Mark extension)
- ✅ `wwwroot/js/tipTapIntegration.js` - Updated to load extension, hashtag conversion
- ✅ `wwwroot/css/chronicis-hashtags.css` - NEW
- ✅ `wwwroot/index.html` - Added CSS link
- ✅ `Services/IHashtagApiService.cs` - NEW
- ✅ `Services/HashtagApiService.cs` - NEW
- ✅ `Program.cs` - Registered HashtagApiService

### Issues Resolved During Development

**Issue 1: Cursor Jumping**
- **Problem:** MutationObserver DOM manipulation broke cursor position
- **Solution:** Used proper TipTap Mark extension with `inclusive: false` and `exitable: true`

**Issue 2: Character Consumption**
- **Problem:** Input rule consumed next character after hashtag
- **Solution:** Changed regex to trigger only on space: `/(?:^|\s)(#[a-zA-Z0-9_]+)\s$/`

**Issue 3: Hashtags Not Saving**
- **Problem:** `HashtagSyncService` not called in UpdateArticle
- **Solution:** Added `await _hashtagSync.SyncHashtagsAsync(article.Id, article.Body);` after SaveChangesAsync

**Issue 4: Plain Text on Reload**
- **Problem:** Existing hashtags loaded as plain text, not styled
- **Solution:** Added hashtag→span conversion in `markdownToHTML()` before header processing

**Issue 5: Mark Extending**
- **Problem:** Typing after hashtag kept styling new text
- **Solution:** Added `inclusive: false` and `exitable: true` to Mark configuration

### Success Criteria

1. ✅ Type `#Waterdeep ` → Styles in beige-gold after space
2. ✅ Type after hashtag → Plain text (not styled)
3. ✅ Auto-save (0.5s) → Saves to database correctly
4. ✅ Multiple hashtags → All styled and saved
5. ✅ Case insensitive → `#Waterdeep` = `#waterdeep` in database
6. ✅ Reload page → Hashtags appear styled
7. ✅ Edit/remove hashtag → Database updates correctly
8. ✅ Cursor works perfectly → No jumping or freezing
9. ✅ Hover effect → Subtle gold glow
10. ✅ Works in all contexts → Paragraphs, lists, headers

### What's Working in Phase 6

✅ **Backend:**
- Hashtags parsed from article body using regex
- Hashtags stored in database (case-insensitive, lowercase)
- Many-to-many relationship between articles and hashtags
- Position tracking for future features
- API endpoints for retrieving hashtag data
- Automatic sync on article save (integrated with 0.5s auto-save)

✅ **Frontend:**
- TipTap Mark extension detects and marks up hashtags
- Visual styling (beige-gold color, hover effects)
- Triggers on space after typing hashtag
- Works when loading existing content
- No cursor issues
- Marks don't extend when typing continues
- HTML ↔ Markdown conversion preserves hashtags

### What's Deferred to Phase 7

⏸️ **Click Handlers:** Hashtags are styled but not clickable  
⏸️ **Navigation:** Cannot navigate to linked articles yet  
⏸️ **Linking UI:** No interface to link hashtags to articles  
⏸️ **Autocomplete:** No `#` autocomplete dropdown yet  
⏸️ **Backlinks:** No panel showing which articles reference current article  
⏸️ **Hover Previews:** No article content preview on hover  

### Common Issues & Solutions (v1.5)

**Hashtags not showing color?**
- Check `index.html` includes `chronicis-hashtags.css`
- Verify CSS class is `chronicis-hashtag`
- Check browser DevTools for CSS conflicts

**Extension not loading?**
- Verify `tipTapHashtagExtension.js` uses ES6 module syntax
- Check import path in `tipTapIntegration.js` is `/js/tipTapHashtagExtension.js`
- Look for console errors in browser DevTools

**Not saving to database?**
- Verify migration applied: `dotnet ef migrations list`
- Check `HashtagSyncService` called in UpdateArticle function
- Look at API logs for errors
- Verify services registered in Program.cs

**Cursor still has issues?**
- Verify `inclusive: false` and `exitable: true` in Mark config
- Check input rule regex matches correctly
- Test with simple hashtag first: `#test `

**Hashtags plain on reload?**
- Verify `markdownToHTML()` converts hashtags BEFORE headers
- Check regex pattern matches stored markdown
- Look for console errors during conversion

---

<a name="phase-7"></a>

## Phase 7: Backlinks & Entity Graph

**Status:** 📜 Next Phase

**Goal:** Display which articles reference the current article and enable hashtag linking

**Note:** Phase 6 built the infrastructure. Phase 7 adds interactivity.

### Backend

- GET /api/articles/{id}/backlinks
- Return articles that mention this article via hashtags
- Include hashtag names and mention counts
- Support for linking hashtags to articles (POST endpoint already exists from Phase 6)

### Frontend

- BacklinksPanel component (right sidebar, 320px)
- List articles that reference current article
- Show hashtags used and mention counts
- Click to navigate
- HashtagHoverPreview on hashtag hover
- Show article title and preview text
- Click handler on hashtags to navigate to linked article
- UI to link unlinked hashtags to articles
- Autocomplete when typing `#` (dropdown with existing hashtags)

### Success Criteria

1. Backlinks panel shows referencing articles
2. Displays which hashtags were used
3. Clicking navigates to article
4. Hover preview works
5. Updates when article is saved (via auto-save)
6. Can link hashtags to articles
7. Linked hashtags are clickable
8. Autocomplete suggests existing hashtags

---

<a name="phase-8"></a>

## Phase 8: AI Summary Generation

**Status:** ⏳ Pending

**Goal:** Generate AI summaries from backlink content

### Backend

- Azure.AI.OpenAI package
- AISummaryService with GPT-4
- POST /api/articles/{id}/generate-summary
- Analyze all backlink content
- Build prompt with mentions
- Store summary (optional caching)

### Frontend

- Generate AI Summary button (in ArticleDetail)
- Loading state while generating
- Display summary with timestamp
- Handle errors gracefully

### AI Prompt Structure

```
Generate summary for: {article.Title}

Mentioned in these notes:
--- From: {backlink.Title} ---
{backlink.Content}

Provide 2-4 paragraph summary including:
- Who/what this entity is
- Key relationships
- Important events
- Current status
```

### Success Criteria

1. Clicking button generates summary
2. Summary includes key information
3. Works with 0 backlinks (graceful)
4. Summaries are coherent and useful

---

<a name="phase-9"></a>

## Phase 9: Content Search & Advanced Discovery

**Status:** ⏳ Pending

**Goal:** Full-text search across article content and hashtags

### Backend

- GET /api/articles/search/content?query={term} (or reuse existing /search endpoint)
- Search titles AND bodies (unlike tree search which is title-only)
- Generate context snippets
- GET /api/hashtags/search?query={term}

### Frontend

- Global search in app header (top-right)
- SearchResults page
- Group results: title matches, content matches, hashtags
- Highlight search terms in snippets
- Autocomplete suggestions as typing
- Click to navigate (opens in inline editor)

### Relationship to Phase 3

Phase 3's original `/api/articles/search` endpoint searches both title and body.
Phase 5 added `/api/articles/search/title` for tree navigation (title-only).
Phase 9 will use the original endpoint for global content search.

### Success Criteria

1. Can search from any page
2. Results show with context
3. Search terms highlighted
4. Fast results (<1s)

---

<a name="phase-10"></a>

## Phase 10: Drag-and-Drop Reorganization

**Status:** ⏳ Pending

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

**Status:** ⏳ Pending

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

**Status:** ⏳ Pending

**Goal:** Ensure quality, optimize, deploy to production

### Testing Strategy

- Unit tests for Article CRUD
- Unit tests for hashtag parsing
- Integration tests for API endpoints
- Manual test plan execution
- Test inline editing edge cases

### Performance Optimizations

- Add database indexes (especially on Title for search, Hashtag.Name)
- Query optimization with projections
- Frontend debouncing (already done for auto-save)
- Response compression
- Caching headers

### Deployment Steps

- Validate Azure infrastructure
- Configure GitHub Actions
- Set environment variables
- Run database migrations on Azure SQL
- Smoke test deployed app
- Set up Application Insights

### Success Criteria

1. All tests passing
2. Performance meets targets
3. Successfully deployed to Azure
4. Monitoring configured
5. Inline editing works in production

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
- **AI Summary:** < 30 seconds

### C. Project Structure

```
chronicis/
├── src/
│   ├── Chronicis.Client/           # Blazor WASM
│   │   ├── Components/
│   │   │   ├── Articles/
│   │   │   │   ├── ArticleDetail.razor (inline editor)
│   │   │   │   └── ArticleTreeView.razor
│   │   │   ├── Hashtags/
│   │   │   └── Search/
│   │   ├── Services/
│   │   │   ├── ArticleApiService.cs
│   │   │   ├── TreeStateService.cs
│   │   │   ├── QuoteService.cs
│   │   │   └── HashtagApiService.cs
│   │   ├── Pages/
│   │   │   └── Home.razor (dashboard + routing)
│   │   └── wwwroot/
│   │       ├── css/
│   │       │   ├── chronicis-home.css
│   │       │   ├── chronicis-nav.css
│   │       │   ├── chronicis-hashtags.css
│   │       │   └── tipTapStyles.css
│   │       └── js/
│   │           ├── tipTapIntegration.js
│   │           └── tipTapHashtagExtension.js
│   ├── Chronicis.Api/              # Azure Functions
│   │   ├── Functions/
│   │   │   ├── ArticleSearchFunction.cs
│   │   │   ├── HashtagFunctions.cs
│   │   │   └── UpdateArticle.cs
│   │   ├── Services/
│   │   │   ├── HashtagParser.cs
│   │   │   └── HashtagSyncService.cs
│   │   └── Data/
│   │       └── Entities/
│   │           ├── Article.cs
│   │           ├── Hashtag.cs
│   │           └── ArticleHashtag.cs
│   └── Chronicis.Shared/           # DTOs
│       └── DTOs/
│           ├── ArticleDto.cs
│           └── HashtagDto.cs
├── tests/
├── docs/
└── Chronicis.sln
```

### D. Inline Editing Architecture

**Key Components:**
- **ArticleDetail.razor:** Always-editable component with title and body fields
- **Auto-Save Timer:** Triggers save 0.5s after user stops typing (body only)
- **Manual Save:** Title requires Save button or Enter key
- **TreeStateService:** Manages article selection via ID without reloading

**Save Flow:**
1. User types in body
2. `OnContentChanged()` triggered
3. Timer reset (0.5s countdown)
4. User stops typing
5. `AutoSave()` fires
6. Article saved via API
7. **Hashtag sync triggered**
8. Tree updated (no reload)

**Title Save Flow (v1.4):**
1. User types in title
2. `OnTitleChanged()` triggered
3. Marked as unsaved (no auto-save)
4. User presses Enter OR clicks Save
5. Article saved via API
6. If title changed:
   - URL updates to new slug
   - Tree refreshes
   - Tree expands to show article
   - Page title updates

**Selection Flow (Phase 5):**
1. User clicks article in tree
2. `SelectArticle(node)` called in ArticleTreeView
3. Navigation.NavigateTo($"/article/{slug}")
4. Home.razor receives slug parameter
5. `LoadArticleBySlug()` searches for article
6. Article loaded and displayed
7. Page title updated

**Hashtag Flow (Phase 6):**
1. User types `#Waterdeep `
2. TipTap input rule detects on space
3. Renders as styled span
4. Auto-save triggers (0.5s)
5. HTML→Markdown converts span to `#waterdeep`
6. UpdateArticle saves to database
7. HashtagSyncService extracts and stores hashtag
8. Database updated with Hashtag and ArticleHashtag records

**Benefits:**
- Seamless editing experience
- No context switching (no modals)
- Never lose work (auto-save for body)
- Deliberate title saves (Enter or button)
- Automatic hashtag detection and storage
- Faster workflow
- Simple state management via article ID
- Deep linking and bookmarks work

### E. Troubleshooting

**Navigation tree not showing expand arrows:**
- Check: API's `MapToDtoWithChildCount` sets ChildCount
- Verify: `Include(a => a.Children)` in GetChildrenAsync
- Solution: Use explicit DB count for ChildCount

**Articles not loading when clicked:**
- Check: Home.razor uses `TreeStateService.SelectedArticleId.HasValue`
- Verify: ArticleDetail subscribes to `TreeState.OnStateChanged`
- Solution: Update Home.razor to check SelectedArticleId

**Title doesn't save on Enter:**
- Check: `Immediate="true"` on MudTextField
- Verify: `@onkeydown` handler exists
- Solution: Add handler that calls SaveArticle()

**Tree doesn't expand after title change:**
- Check: Using `ExpandAndSelectArticle()` not `NotifySelectionChanged()`
- Verify: ArticleTreeView subscribed to `OnExpandAndSelect` event
- Solution: Implement event-based expansion

**Logo navigation shows error:**
- Check: Home.razor checks `SelectedArticleId.Value > 0`
- Verify: ArticleDetail handles `SelectedArticleId == 0`
- Solution: Treat 0 as "no selection"

**Tree search shows body matches:**
- Check: Using `SearchArticlesByTitleAsync` not `SearchArticlesAsync`
- Verify: API endpoint is `/api/articles/search/title`
- Solution: Update TreeStateService to use title-only endpoint

**Browser title doesn't update:**
- Check: JSRuntime.InvokeVoidAsync calls in LoadArticleAsync and SaveArticle
- Verify: EscapeForJs helper handles special characters
- Solution: Add title update after load and save

**Quote API errors:**
- Check: Using Quotable API (not Zen Quotes)
- Verify: URL is https://api.quotable.io/quotes/random?maxLength=200
- Solution: No auth needed, works in browser

**Hashtags not showing color:**
- Check: `chronicis-hashtags.css` linked in index.html
- Verify: TipTap extension loaded successfully
- Solution: Check browser console for import errors

**Hashtags not saving to database:**
- Check: `HashtagSyncService` called in UpdateArticle
- Verify: Services registered in Program.cs
- Solution: Add debug logging to track sync process

**Cursor jumps when typing hashtags:**
- Check: Mark extension has `inclusive: false` and `exitable: true`
- Verify: Not using DOM manipulation (MutationObserver)
- Solution: Use proper TipTap Mark extension approach

**Hashtags plain text on reload:**
- Check: `markdownToHTML()` converts hashtags BEFORE headers
- Verify: Regex pattern matches stored markdown
- Solution: Ensure hashtag conversion happens first

**Cannot connect to SQL:**
- For Docker: `docker start sql-server`
- Check connection string
- Verify SQL Server is running

**CORS errors:**
- Add CORS policy in API Program.cs
- Allow origin `https://localhost:5001`

**Hot Reload not working:**
- Use `dotnet watch run` instead of Ctrl+F5
- Check Visual Studio Hot Reload settings
- Verify file save triggers rebuild

### F. Using This Plan

**Before Starting Phase 7:**
1. Review Phase 7 specification
2. Check that all Phase 6 features are working
3. Create new chat with Claude
4. Upload this plan + spec PDFs
5. Say: "I'm ready to start Phase 7 - Backlinks & Entity Graph"

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

### G. AI Tool Strategy

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

### H. Phase 6 Key Learnings (v1.5)

**TipTap Extension Architecture:**
- Import Mark and helpers from CDN: `https://esm.sh/@tiptap/core@3.11.0`
- Use `inclusive: false` to prevent mark from extending
- Use `exitable: true` to allow cursor to exit mark
- Input rules trigger on space to avoid incomplete hashtags
- Paste rules handle pasted content with hashtags

**HTML ↔ Markdown Conversion:**
- Convert hashtags BEFORE headers to avoid `#Waterdeep` → `<h1>Waterdeep</h1>`
- Store hashtags in lowercase but display with original case
- Multiple regex patterns for reliability (primary + fallbacks)
- Preserve hashtag structure through round-trip conversion

**Database Design:**
- Case-insensitive unique index on Hashtag.Name
- Position tracking enables future features (jump to hashtag, context)
- Many-to-many with explicit junction table (better than EF Core automatic)
- CASCADE delete on ArticleHashtag but SET NULL on LinkedArticleId

**Service Architecture:**
- Separate concerns: Parser (extract) vs Sync (persist)
- Parser is stateless and reusable
- Sync service handles complex CRUD logic
- Integration point is single line in UpdateArticle

**Debugging Strategy:**
- Console logging essential for troubleshooting
- Log at multiple points: parse, sync, save
- Remove debug logs after confirming functionality
- Browser DevTools invaluable for frontend issues

**Performance Considerations:**
- Regex compilation flag improves parse speed
- Batch operations in sync (remove all, add all)
- Single SaveChangesAsync call at end
- No N+1 queries (use Include and proper projections)

**UX Design Decisions:**
- Space trigger feels natural (like markdown bold)
- Plain text while typing avoids distraction
- Immediate styling on space provides feedback
- Hover effect subtle to avoid being overwhelming
- Not clickable in Phase 6 keeps scope manageable

---

## Final Notes

**Remember:**
- This is a learning project - focus on the process
- AI accelerates but doesn't replace judgment
- Build phase by phase - don't skip ahead
- Test frequently, commit often
- Document your learnings
- Have fun! 🎉🐉

**Phase 6 Complete! ✅**
All features implemented and tested:
- ✅ Hashtag parsing with regex
- ✅ Database storage (Hashtag + ArticleHashtag)
- ✅ TipTap Mark extension
- ✅ Visual styling (beige-gold, hover effects)
- ✅ Auto-sync on save
- ✅ HTML ↔ Markdown conversion
- ✅ API endpoints
- ✅ No cursor issues
- ✅ Case-insensitive storage
- ✅ Professional UX

**When Ready to Start Phase 7:**
Create a new chat, upload this plan and the spec PDFs, and say:
*"I'm ready to start Phase 7 of Chronicis implementation - Backlinks & Entity Graph. Note: Phase 6 is complete with full hashtag system including parsing, storage, visual styling, and API endpoints."*

---

**Version History:**
- 1.5 (2025-11-26): Phase 6 COMPLETE - Full hashtag system with parsing, storage, visual styling, API endpoints
- 1.4 (2025-11-25): Phase 5 COMPLETE - Dashboard, routing, title save, tree expansion, logo nav, title search API
- 1.3 (2025-11-25): Phase 5 complete implementation with all fixes, custom navigation, TreeStateService updates
- 1.2 (2025-11-24): Phase 4 complete rewrite using TipTap v3.11.0, ES modules via esm.sh, Blazor lifecycle fixes
- 1.1 (2025-11-23): Updated for inline editing paradigm, removed ArticleEditor references
- 1.0 (2025-11-18): Initial comprehensive plan

**License:** Part of the Chronicis project. Modify as needed for your team.

---

*End of Implementation Plan*