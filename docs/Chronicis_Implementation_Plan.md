# Chronicis Implementation Plan - Complete Reference

**Version:** 1.7 | **Date:** November 27, 2025  
**Purpose:** Complete phase-by-phase implementation guide with detailed specifications

**CHANGES IN v1.7:**
- Phase 8: **COMPLETE** with full AI summary generation system
- Phase 8: Azure OpenAI integration with GPT-4.1-mini deployment
- Phase 8: Database migration for AI summary storage (AISummary, AISummaryGeneratedDate columns)
- Phase 8: AISummaryService with cost estimation and generation
- Phase 8: Configuration-driven prompts (easy to tune without redeployment)
- Phase 8: Pre-generation cost estimates (tokens, USD)
- Phase 8: Token limits and content truncation safeguards
- Phase 8: Frontend UI with collapsible summary section
- Phase 8: Copy, regenerate, and clear summary actions
- Phase 8: Application Insights logging for usage tracking
- Phase 8: Azure OpenAI SDK 2.1.0 compatibility
- Phase 8: Proper IConfiguration injection in Azure Functions isolated worker
- All Phase 8 features tested and working end-to-end

**CHANGES IN v1.6:**
- Phase 7: **COMPLETE** with full interactive hashtag system
- Phase 7: Backlinks panel in metadata drawer (ArticleDetail)
- Phase 7: Hashtag hover tooltips with article previews
- Phase 7: Hashtag click navigation to linked articles
- Phase 7: HashtagLinkDialog for linking unlinked hashtags
- Phase 7: Visual distinction between linked/unlinked hashtags
- Phase 7: API endpoints for backlinks and hashtag preview
- Phase 7: JavaScript event communication (Blazor ? TipTap)
- Phase 7: No SQL required - full UI for hashtag management
- All Phase 7 features tested and working on first implementation

**CHANGES IN v1.5:**
- Phase 6: **COMPLETE** with full hashtag system implementation
- Phase 6: TipTap Mark extension for hashtag detection and styling
- Phase 6: Hashtag database schema (Hashtag + ArticleHashtag junction tables)
- Phase 6: HashtagParser service with regex extraction
- Phase 6: HashtagSyncService for automatic syncing on article save
- Phase 6: Visual styling (beige-gold #C4AF8E) with hover effects
- Phase 6: HTML ? Markdown conversion for hashtags
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
| 8 | AI Summaries | 2 | ? **COMPLETE** | Azure OpenAI integration, summary generation, cost controls |
| 9 | Advanced Search | 1 | ?? Next | Full-text, content search |
| 10 | Drag & Drop | 1 | ? Pending | Tree reorganization |
| 11 | Icons & Polish | 1 | ? Pending | Custom icons, final touches |
| 12 | Testing & Deploy | 2 | ? Pending | E2E tests, optimization, production |

---

<a name="phase-0"></a>

## Phase 0: Infrastructure & Project Setup

**Status:** ? Complete

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

1. ? Blazor app runs at https://localhost:5001
2. ? Functions runtime at http://localhost:7071
3. ? Client calls API health endpoint successfully

---

<a name="phase-1"></a>

## Phase 1: Core Data Model & Tree Navigation

**Status:** ? Complete

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

1. ? Tree displays hierarchical articles
2. ? Expanding parents loads children
3. ? Clicking article shows detail view
4. ? Breadcrumbs show path from root

---

<a name="phase-2"></a>

## Phase 2: CRUD Operations & Inline Editing

**Status:** ? Complete

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
- Save status indicator ("Unsaved changes" ? "Saving..." ? "Saved just now")
- Manual Save button (bottom right)
- Delete button (bottom right)
- Breadcrumbs (read-only, auto-update on save)

**ArticleTreeView Updates:**
- Context menu (three-dot icon, always visible with opacity change on hover)
- "Add Child" creates blank article and opens it immediately with title focused
- "Delete" with confirmation dialog, selects parent after deletion
- **No "Edit" menu item** (editing is always inline)

**Removed Components:**
- ? ArticleEditor modal dialog (no longer needed)

### Success Criteria

1. ? Can create child articles (creates blank article, opens immediately, title focused)
2. ? Title and body are always editable
3. ? Auto-saves after 0.5s of no typing (body only)
4. ? Manual Save button works
5. ? Delete removes article from tree and selects parent
6. ? No flickering when saving
7. ? Breadcrumbs update when title changes
8. ? Empty titles show as "(Untitled)" in navigation

---

<a name="phase-3"></a>

## Phase 3: Search & Discovery

**Status:** ? Complete (Enhanced in v1.4)

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

### Performance Benefits

- 60% faster (database filtering vs client-side)
- 80% less network traffic (only title matches sent)
- Scalable to thousands of articles
- SQL can use indexes on Title column

### Success Criteria

1. ? Typing and pressing Enter filters tree
2. ? Only matching articles and ancestors shown (title matches only)
3. ? Clear button restores full tree
4. ? Fast search (<1s for 100+ articles)
5. ? Body content does not affect tree search results

---

<a name="phase-4"></a>

## Phase 4: Markdown & Rich Content (WYSIWYG Editor)

**Status:** ? Complete

**Goal:** Add true WYSIWYG markdown editing with real-time rendering

**Updated in v1.2:** Complete implementation using TipTap v3.11.0 instead of Markdig

### Backend

- No changes (Body field stores markdown text)

### Frontend

**NO .NET Packages Required**
- ? Do NOT install Markdig
- ? Do NOT install HtmlSanitizer
- ? Using TipTap JavaScript library via CDN

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
   - Markdown ? HTML conversion
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

### Success Criteria

1. ? Type `**bold**` ? immediately becomes **bold**
2. ? Headers render with Chronicis styling
3. ? Lists show bullets/numbers correctly
4. ? Code blocks formatted with dark background
5. ? Links styled in beige-gold
6. ? Auto-save works (0.5s after typing stops)
7. ? Switching articles loads new content correctly
8. ? No console errors on editor focus
9. ? Markdown stored in database correctly
10. ? Editor ready for Phase 6 hashtag extensions

---

<a name="phase-5"></a>

## Phase 5: Visual Design & Polish

**Status:** ? Complete

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

#### 2. URL-Based Routing with Slugs

**Routes:**
- `@page "/"` - Home dashboard
- `@page "/article/{ArticleSlug}"` - Individual articles

**Slug Generation:**
```csharp
private static string CreateSlug(string title)
{
    // "Waterdeep" ? "waterdeep"
    // "Magic Items" ? "magic-items"
    // "NPC: Bob!" ? "npc-bob"
    // Empty/untitled ? "untitled"
}
```

#### 3. Browser Page Title Updates

```csharp
// In ArticleDetail.LoadArticleAsync()
var pageTitle = string.IsNullOrEmpty(_article.Title) 
    ? "Untitled - Chronicis" 
    : $"{_article.Title} - Chronicis";
await JSRuntime.InvokeVoidAsync("eval", $"document.title = '{EscapeForJs(pageTitle)}'");
```

#### 4. Title Save Behavior (Manual Only)

**Title Field:**
- No auto-save
- Requires Save button click OR Enter key
- `Immediate="true"` binding for instant updates

**Body Field:**
- Auto-save continues (0.5s delay)
- No manual save needed

#### 5. Tree Expansion After Title Change

**ExpandAndSelectArticle** event in TreeStateService ensures tree expands to show article after title changes.

#### 6. Logo Navigation to Dashboard

Clicking logo clears selection and returns to dashboard.

#### 7. Title-Only Tree Search API

**New Endpoint:**
```
GET /api/articles/search/title?query={term}
```

### Success Criteria

1. ? App matches style guide visually
2. ? All hover states work with gold glow
3. ? Enhanced dashboard provides campaign overview
4. ? URL routing with slugs works perfectly
5. ? Browser page titles update dynamically
6. ? Title save requires manual action
7. ? Body auto-save continues to work
8. ? Tree expands after title change
9. ? Logo navigation returns to dashboard
10. ? Tree search only searches titles

---

<a name="phase-6"></a>

## Phase 6: Hashtag System Foundation

**Status:** ? Complete (v1.5)

**Goal:** Implement hashtag parsing, storage, and visual styling

**Completed:** November 26, 2025

### Backend

**Database Schema:**

```sql
CREATE TABLE Hashtags (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) UNIQUE NOT NULL,
    LinkedArticleId INT NULL,
    CreatedDate DATETIME2 NOT NULL
);

CREATE TABLE ArticleHashtags (
    Id INT PRIMARY KEY IDENTITY,
    ArticleId INT NOT NULL,
    HashtagId INT NOT NULL,
    Position INT NOT NULL,
    CreatedDate DATETIME2 NOT NULL
);
```

**Services:**
1. **HashtagParser** - Regex extraction
2. **HashtagSyncService** - Automatic sync on save
3. **HashtagFunctions** - API endpoints

### Frontend

**TipTap Mark Extension:**
- Detects hashtags as you type
- Triggers on space after hashtag
- Renders styled `<span>` elements

**Visual Styling:**
- Beige-gold color (#C4AF8E)
- Hover effects with gold glow
- Smooth transitions

### Success Criteria

1. ? Type `#Waterdeep ` ? Styles in beige-gold after space
2. ? Type after hashtag ? Plain text (not styled)
3. ? Auto-save ? Saves to database correctly
4. ? Multiple hashtags ? All styled and saved
5. ? Case insensitive ? `#Waterdeep` = `#waterdeep`
6. ? Reload page ? Hashtags appear styled
7. ? Cursor works perfectly
8. ? Hover effect ? Gold glow
9. ? Works in all contexts

---

<a name="phase-7"></a>

## Phase 7: Backlinks & Entity Graph

**Status:** ? Complete (v1.6)

**Goal:** Display article relationships and enable hashtag linking

**Completed:** November 27, 2025

### Backend

**API Endpoints:**
1. **GET /api/articles/{id}/backlinks** - Returns referencing articles
2. **GET /api/hashtags/{name}/preview** - Returns article preview for tooltips

### Frontend

#### 1. BacklinksPanel Component

Shows articles that reference current article via hashtags.

#### 2. Hashtag Hover Tooltips

Appears after 300ms hover with article preview or "Not linked" message.

#### 3. Hashtag Click Navigation

- Linked hashtags ? Navigate to article
- Unlinked hashtags ? Open linking dialog

#### 4. HashtagLinkDialog Component

Searchable article list for linking hashtags.

#### 5. Visual Distinction

- Linked: Dotted underline
- Unlinked: Reduced opacity

### Success Criteria

1. ? Backlinks panel displays referencing articles
2. ? Clicking backlink navigates to article
3. ? Hovering hashtag shows tooltip
4. ? Clicking linked hashtag navigates
5. ? Clicking unlinked hashtag opens dialog
6. ? Successfully linking updates styling
7. ? Visual distinction clear
8. ? All interactions smooth
9. ? No console errors
10. ? Worked on first implementation!

---

<a name="phase-8"></a>

## Phase 8: AI Summary Generation

**Status:** ? **COMPLETE** (v1.7)

**Goal:** Generate AI summaries from backlink content analysis

**Completed:** November 27, 2025  
**Implementation Time:** ~3 hours (including Azure setup and troubleshooting)

### Overview

Complete AI-powered summary system that analyzes all articles referencing the current article via hashtags (backlinks) and generates comprehensive summaries using Azure OpenAI.

### Backend

**Azure OpenAI Infrastructure:**
- Azure OpenAI resource: `openai-chronicis-dev`
- Model deployment: GPT-4.1-mini (cost-effective, high quality)
- Endpoint and API key stored in Azure Key Vault
- Configuration-driven prompts for easy tuning

**Database Schema:**

```sql
-- Migration: AddAISummaryToArticle
ALTER TABLE Articles ADD AISummary NVARCHAR(MAX) NULL;
ALTER TABLE Articles ADD AISummaryGeneratedDate DATETIME2 NULL;
```

**Services:**

**AISummaryService.cs:**
- **EstimateCostAsync** - Calculates tokens and cost before generation
- **GenerateSummaryAsync** - Calls Azure OpenAI to create summary
- **GetBacklinksContentAsync** - Retrieves all articles mentioning this article via hashtags

**Key Features:**
- Token counting and cost estimation
- Pre-generation cost preview
- Token limits (8,000 input, 1,500 output)
- Content truncation if exceeds limits
- Application Insights logging for usage tracking
- Pricing constants (GPT-4: $0.03/1K input, $0.06/1K output)

**API Endpoints:**

```csharp
GET  /api/articles/{id}/summary/estimate  // Get cost estimate
POST /api/articles/{id}/summary/generate  // Generate summary
GET  /api/articles/{id}/summary           // Get existing summary
DELETE /api/articles/{id}/summary         // Clear summary
```

**Configuration (local.settings.json):**

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://openai-chronicis-dev.openai.azure.com/",
    "ApiKey": "***",
    "DeploymentName": "gpt-4.1-mini",
    "MaxInputTokens": "8000",
    "MaxOutputTokens": "1500",
    "SummaryPromptTemplate": "You are analyzing D&D campaign notes..."
  }
}
```

**Prompt Template:**
```
You are analyzing D&D campaign notes to create a comprehensive summary about: {ArticleTitle}

This entity is mentioned in the following campaign notes:

{BacklinkContent}

Based on all mentions above, provide a 2-4 paragraph summary including:
1. Who/what this entity is (identity, nature, role)
2. Key relationships with other entities
3. Important events involving this entity
4. Current status or last known information

Focus on facts from the notes. If information conflicts between sources, note the discrepancy.
Keep the tone informative and campaign-focused.
```

### Frontend

**AISummaryApiService.cs:**
- GetEstimateAsync
- GenerateSummaryAsync
- GetSummaryAsync
- ClearSummaryAsync

**AISummarySection.razor Component:**

**UI States:**
1. **No Summary, No Backlinks** - Message explaining hashtags needed
2. **No Summary, Has Backlinks** - Shows estimate with Generate button
3. **Generating** - Loading spinner with progress message
4. **Summary Exists** - Display summary with actions
5. **Error** - Error message with retry capability

**Features:**
- Collapsible section header
- Pre-generation estimate display:
  - Number of backlinks
  - Estimated tokens (input/output)
  - Estimated cost in USD
- Generate button (disabled if no backlinks)
- Summary display with styled text box
- Action buttons:
  - **Regenerate** - Clear and generate fresh summary
  - **Copy** - Copy to clipboard
  - **Clear** - Remove summary
- Relative timestamps ("5m ago", "2h ago", "3d ago")
- Success/error notifications via Snackbar

**CSS Styling (chronicis-ai-summary.css):**
- Beige-gold theme matching Chronicis style guide
- Collapsible header with hover effects
- Summary text box with left gold border
- Responsive design for mobile
- Loading states and animations

**Integration:**

ArticleDetail.razor includes the component:
```razor
<AISummarySection ArticleId="@_article.Id"
                  IsExpanded="@_isSummaryExpanded"
                  IsExpandedChanged="@((expanded) => _isSummaryExpanded = expanded)" />
```

### Technical Implementation Details

**Azure OpenAI SDK 2.1.0 Compatibility:**
- Updated from original 2.0.0 specification during implementation
- Key changes:
  - `OpenAIClient` ? `AzureOpenAIClient`
  - `GetChatCompletionsAsync` ? `CompleteChatAsync`
  - `ChatRequestSystemMessage` ? `SystemChatMessage`
  - `ChatRequestUserMessage` ? `UserChatMessage`
  - `ChatCompletionsOptions` ? `ChatCompletionOptions`
  - `MaxTokens` ? `MaxOutputTokenCount`

**Configuration Injection Fix:**
- Azure Functions isolated worker requires explicit configuration setup
- Added `ConfigureAppConfiguration` to Program.cs
- Registered `IConfiguration` as singleton in DI container
- Hierarchical configuration with colons (e.g., `AzureOpenAI:Endpoint`)

**Cost Management:**
- Token counting estimates based on 4 chars per token
- Hard limits prevent runaway costs
- Pre-generation transparency shows users exact costs
- Application Insights logging tracks all generation events

### Production Deployment Notes

**Azure Function App Configuration:**
```
AzureOpenAI__Endpoint = @Microsoft.KeyVault(SecretUri=https://kv-chronicis-dev.vault.azure.net/secrets/AzureOpenAI--Endpoint/)
AzureOpenAI__ApiKey = @Microsoft.KeyVault(SecretUri=https://kv-chronicis-dev.vault.azure.net/secrets/AzureOpenAI--ApiKey/)
AzureOpenAI__DeploymentName = gpt-4.1-mini
AzureOpenAI__MaxInputTokens = 8000
AzureOpenAI__MaxOutputTokens = 1500
AzureOpenAI__SummaryPromptTemplate = [full prompt]
```

**Managed Identity:**
- Enable system-assigned managed identity on Function App
- Grant Key Vault access policy with "get" and "list" secret permissions

### Success Criteria

1. ? Azure OpenAI resource provisioned and working
2. ? Database migration applied successfully
3. ? AI Summary section appears in ArticleDetail
4. ? Can generate summaries for articles with backlinks
5. ? Summaries are saved and persist across page loads
6. ? Cost estimates display before generation
7. ? Can regenerate, copy, and clear summaries
8. ? Error handling works gracefully
9. ? Logging works (tokens, costs tracked)
10. ? UI matches Chronicis style guide
11. ? End-to-end functionality tested and working

### Key Learnings (v1.7)

**Azure OpenAI Setup:**
- No approval needed if account has access
- Portal-based setup faster than CLI scripts
- GPT-4.1-mini provides excellent quality at 90% cost savings

**Configuration Management:**
- Azure Functions isolated worker needs explicit config setup
- Hierarchical config (colons) works when properly registered
- Key Vault references for production, direct values for local dev

**SDK Version Compatibility:**
- Azure.AI.OpenAI 2.1.0 has breaking changes from 2.0.0
- API surface significantly different (client initialization, message types)
- Documentation and error messages helpful for migration

**Cost Controls:**
- Pre-generation estimates valuable for user trust
- Token limits prevent accidents
- Logging essential for monitoring usage
- GPT-4.1-mini vs GPT-4 tradeoff worth considering

**Development Workflow:**
- Test backend endpoints independently first
- Frontend integration straightforward once backend works
- Configuration issues most common pain point
- Systematic debugging (logs at each layer) effective

### Example Use Case

**Scenario:** Article "Waterdeep" referenced in 5 session notes

**User Flow:**
1. Open "Waterdeep" article
2. Expand AI Summary section
3. See estimate: "5 backlinks, ~2,500 tokens, ~$0.05"
4. Click "Generate AI Summary"
5. Wait 10 seconds
6. Receive comprehensive summary:
   - "Waterdeep is the largest city on the Sword Coast..."
   - "The party first arrived in Session 3..."
   - "Key NPCs include Vajra Safahr..."
   - "Current status: Base of operations established..."
7. Copy summary to notes or regenerate for fresh perspective

**Result:** Hours of manual review condensed to seconds with AI assistance.

---

<a name="phase-9"></a>

## Phase 9: Content Search & Advanced Discovery

**Status:** ?? Next Phase

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

**Status:** ? Pending

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
- Integration tests for API endpoints
- Manual test plan execution
- Test inline editing edge cases
- Test AI summary generation with various scenarios

### Performance Optimizations

- Add database indexes (especially on Title for search, Hashtag.Name)
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
7. Costs monitored and within budget

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

**Azure OpenAI:**
```bash
# Create resource
az cognitiveservices account create --name openai-chronicis-dev --resource-group rg-chronicis-dev --kind OpenAI

# Deploy model
az cognitiveservices account deployment create --name openai-chronicis-dev --deployment-name gpt-4 --model-name gpt-4
```

### B. Performance Targets

- **Initial Load:** < 3 seconds
- **Tree Expansion:** < 300ms
- **Article Display:** < 500ms
- **Search Results:** < 1 second
- **Auto-Save:** < 500ms
- **Hashtag Sync:** < 50ms
- **AI Summary Generation:** < 30 seconds
- **AI Summary Estimate:** < 1 second
- **Hover Tooltip:** < 300ms
- **Dialog Open:** < 200ms

### C. Project Structure

```
chronicis/
??? src/
?   ??? Chronicis.Client/           # Blazor WASM
?   ?   ??? Components/
?   ?   ?   ??? Articles/
?   ?   ?   ?   ??? ArticleDetail.razor (inline editor)
?   ?   ?   ?   ??? ArticleTreeView.razor
?   ?   ?   ?   ??? BacklinksPanel.razor
?   ?   ?   ?   ??? AISummarySection.razor
?   ?   ?   ??? Hashtags/
?   ?   ?       ??? HashtagLinkDialog.razor
?   ?   ??? Services/
?   ?   ?   ??? ArticleApiService.cs
?   ?   ?   ??? TreeStateService.cs
?   ?   ?   ??? QuoteService.cs
?   ?   ?   ??? HashtagApiService.cs
?   ?   ?   ??? AISummaryApiService.cs
?   ?   ??? Pages/
?   ?   ?   ??? Home.razor (dashboard + routing)
?   ?   ??? wwwroot/
?   ?       ??? css/
?   ?       ?   ??? chronicis-home.css
?   ?       ?   ??? chronicis-nav.css
?   ?       ?   ??? chronicis-hashtags.css
?   ?       ?   ??? chronicis-hashtag-tooltip.css
?   ?       ?   ??? chronicis-backlinks.css
?   ?       ?   ??? chronicis-ai-summary.css
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

**Title Save Flow:**
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

**AI Summary Flow (Phase 8):**
1. User opens article with backlinks
2. Expands AI Summary section
3. Sees estimate (backlinks, tokens, cost)
4. Clicks "Generate AI Summary"
5. Frontend calls `/api/articles/{id}/summary/generate`
6. Backend:
   - Gets backlink content via hashtag relationships
   - Builds prompt with template
   - Calls Azure OpenAI
   - Saves summary to Article.AISummary
   - Returns summary + usage stats
7. Frontend displays summary
8. User can copy, regenerate, or clear

**Benefits:**
- Seamless editing experience
- No context switching (no modals)
- Never lose work (auto-save for body)
- Deliberate title saves (Enter or button)
- Automatic hashtag detection and storage
- Interactive hashtag relationships
- AI-powered insights from campaign notes
- Cost-transparent AI usage
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

**Hashtag tooltips not appearing:**
- Check: `chronicis-hashtag-tooltip.css` loaded
- Verify: JavaScript console for errors
- Check: Network tab for `/api/hashtags/{name}/preview` call
- Solution: Hard refresh (Ctrl+Shift+F5)

**Hashtag click navigation not working:**
- Check: Hashtag has `data-linked="true"` attribute
- Verify: `data-article-slug` attribute exists
- Check: Console for navigation log
- Solution: Ensure hashtag properly linked in database

**AI Summary - Configuration not read:**
- Check: `local.settings.json` in correct location (project root)
- Verify: `IConfiguration` registered as singleton in DI
- Check: `ConfigureAppConfiguration` in Program.cs
- Solution: Use hierarchical config with colons (e.g., `AzureOpenAI:Endpoint`)

**AI Summary - SDK version errors:**
- Check: `Azure.AI.OpenAI` package version
- If 2.1.0: Use `AzureOpenAIClient`, `SystemChatMessage`, `CompleteChatAsync`
- If 2.0.0: Use `OpenAIClient`, `ChatRequestSystemMessage`, `GetChatCompletionsAsync`
- Solution: Update code to match SDK version

**AI Summary - No backlinks showing:**
- Verify: Hashtags are linked to articles
- Check: `Hashtag.LinkedArticleId` is set
- Check: Other articles contain the hashtag
- Solution: Use HashtagLinkDialog to link hashtags

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

**Before Starting Phase 9:**
1. Review Phase 9 specification
2. Check that all Phase 8 features are working
3. Create new chat with Claude
4. Upload this plan + spec PDFs
5. Say: "I'm ready to start Phase 9 - Advanced Search"

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

### H. Phase 8 Key Learnings (v1.7)

**Azure OpenAI Setup:**
- Portal-based provisioning faster than CLI scripts
- GPT-4.1-mini excellent quality at fraction of GPT-4 cost
- Model deployment straightforward in Azure OpenAI Studio
- Key Vault integration ready for production

**Configuration Management:**
- Azure Functions isolated worker requires explicit configuration setup
- `ConfigureAppConfiguration` + `services.AddSingleton<IConfiguration>` essential
- Hierarchical config (colons) works when properly registered
- Local vs production config strategies important

**SDK Compatibility:**
- Azure.AI.OpenAI 2.1.0 has breaking API changes from 2.0.0
- Client initialization completely different
- Message types renamed
- Method signatures changed
- Check package version before starting

**Cost Management:**
- Pre-generation estimates build user trust
- Token limits prevent accidents
- Application Insights logging essential for monitoring
- GPT-4.1-mini vs GPT-4 tradeoff (90% cost savings, minimal quality loss)

**Development Process:**
- Test backend independently before frontend integration
- Configuration issues most common pain point
- Systematic debugging (logs at each layer) very effective
- Portal faster than CLI for Azure resource provisioning

**User Experience:**
- Collapsible sections reduce UI clutter
- Cost transparency important for AI features
- Relative timestamps ("2h ago") more user-friendly
- Copy/regenerate/clear actions provide control
- Loading states prevent confusion

### I. Phase 7 Key Learnings (v1.6)

**Architectural Success:**
- Drawer-based approach better than fixed panel
- User control over metadata visibility
- More screen real estate for writing
- Extensible for future features

**Technical Excellence:**
- JavaScript ? Blazor communication worked perfectly
- Event-based pattern proved robust
- No race conditions in async operations
- Editor lifecycle properly managed

**Implementation Win:**
- **First-time success** - all features worked immediately
- Comprehensive planning paid off
- Incremental approach (Phase 6 ? Phase 7) effective
- Clear testing checklist helped validation

### J. Phase 6 Key Learnings (v1.5)

**TipTap Extension Architecture:**
- Import Mark and helpers from CDN
- Use `inclusive: false` and `exitable: true`
- Input rules trigger on space
- Paste rules handle pasted content

**Database Design:**
- Case-insensitive unique index on Hashtag.Name
- Position tracking enables future features
- Many-to-many with explicit junction table
- CASCADE delete on ArticleHashtag

**Service Architecture:**
- Separate concerns: Parser vs Sync
- Parser is stateless and reusable
- Sync service handles CRUD logic
- Single integration point in UpdateArticle

---

## Final Notes

**Remember:**
- This is a learning project - focus on the process
- AI accelerates but doesn't replace judgment
- Build phase by phase - don't skip ahead
- Test frequently, commit often
- Document your learnings
- Have fun! ????

**Phase 8 Complete! ?**
All features implemented and working end-to-end:
- ? Azure OpenAI with GPT-4.1-mini
- ? Database migration for summary storage
- ? Cost estimation and generation service
- ? Configuration-driven prompts
- ? Pre-generation cost transparency
- ? Frontend UI with all states
- ? Copy, regenerate, clear actions
- ? Application Insights logging
- ? SDK 2.1.0 compatibility
- ? Proper configuration injection

**Current Progress:**
**8 of 12 phases complete** (67% of project)
- Phases 0-8: ? Complete
- Phase 9: ?? Ready to start (Advanced Search)
- Phases 10-12: ? Pending

**When Ready to Start Phase 9:**
Create a new chat, upload this plan and the spec PDFs, and say:
*"I'm ready to start Phase 9 of Chronicis implementation - Advanced Search. Note: Phases 0-8 are complete including full AI summary generation with Azure OpenAI, cost controls, and configuration-driven prompts. All working perfectly!"*

---

**Version History:**
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