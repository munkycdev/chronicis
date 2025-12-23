# Wiki Links - Implementation Phases (Detailed)

**Version:** 1.2  
**Date:** December 23, 2025  
**Status:** In Progress (Phase WL-10)

**Changelog:**
- v1.2 (Dec 22-23, 2025): Phases WL-1 through WL-9 COMPLETE
- v1.1 (Dec 22, 2025): Initial implementation phases

**Progress:** 9/21 phases complete (43%!)

**Note:** This document breaks the wiki links feature into small, atomic phases suitable for delegation to Claude Sonnet. Each phase is designed to be completable in a single conversation with clear success criteria.

**Reference:** See `WIKI_LINKS_ARCHITECTURE.md` for full design context.

---

## Phase WL-1: ArticleLink Entity & Migration ✅ COMPLETE

**Status:** Complete (December 22, 2025)

**Goal:** Create the database entity for storing wiki links.

**Created:**
- ✅ `Chronicis.Shared/Models/ArticleLink.cs` - Entity with Id, SourceArticleId, TargetArticleId, DisplayText, Position, CreatedAt, navigation properties

**Modified:**
- ✅ `Chronicis.Shared/Models/Article.cs` - Added OutgoingLinks and IncomingLinks navigation properties
- ✅ `Chronicis.Api/Data/ChronicisDbContext.cs` - Added DbSet<ArticleLink> and ConfigureArticleLink method with indexes

**Migration:** `AddArticleLinks` created and applied

**Key Decision:**
- SourceArticle: CASCADE delete (when source deleted, remove its links)
- TargetArticle: NO ACTION delete (SQL Server limitation - allows broken link detection)

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ Migration file created with CreateTable for ArticleLinks
- ✅ Foreign keys to Articles table in both directions
- ✅ Migration applied to database

---

## Phase WL-2: Link Parser Service ✅ COMPLETE

**Status:** Complete (December 22, 2025)

**Goal:** Create a service that extracts wiki links from markdown text.

**Created:**
- ✅ `Chronicis.Api/Services/ILinkParser.cs` - Interface with `ParseLinks()` method and `ParsedLink` record
- ✅ `Chronicis.Api/Services/LinkParser.cs` - Regex-based implementation

**Modified:**
- ✅ `Chronicis.Api/Program.cs` - Registered ILinkParser as scoped service

**Implementation Details:**
- Regex pattern: `\[\[([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:\|([^\]]+))?\]\]`
- Gracefully handles null/empty body
- Uses `Guid.TryParse()` to skip invalid GUIDs
- Tracks position via `Match.Index`

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ Parser extracts guid from `[[a1b2c3d4-e5f6-7890-abcd-ef1234567890]]`
- ✅ Parser extracts guid and display text from `[[a1b2c3d4-e5f6-7890-abcd-ef1234567890|Waterdeep]]`
- ✅ Parser returns correct position (character offset) for each match
- ✅ Parser handles multiple links in one body
- ✅ Parser ignores malformed links gracefully

---

## Phase WL-3: Link Sync Service ✅ COMPLETE

**Status:** Complete (December 22, 2025)

**Goal:** Create a service that updates the ArticleLink table when an article is saved.

**Created:**
- ✅ `Chronicis.Api/Services/ILinkSyncService.cs` - Interface with `SyncLinksAsync()` method
- ✅ `Chronicis.Api/Services/LinkSyncService.cs` - Implementation with delete-then-insert strategy

**Modified:**
- ✅ `Chronicis.Api/Program.cs` - Registered ILinkSyncService as scoped service

**Implementation Details:**
- Delete-then-insert approach for simplicity
- Uses `RemoveRange()` and `AddRangeAsync()` for efficiency
- All operations in single transaction via `SaveChangesAsync()`
- Logs metrics: removed count and added count
- Sets `CreatedAt` to `DateTime.UtcNow`

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ Service deletes old links before adding new ones
- ✅ Service creates ArticleLink rows with correct source, target, display text, position

---

## Phase WL-4: Integrate Link Sync with Article Update ✅ COMPLETE

**Status:** Complete (December 22, 2025)

**Goal:** Call link sync whenever an article body is saved.

**Modified:**
- ✅ `Chronicis.Api/Functions/UpdateArticle.cs` - Injected ILinkSyncService and called SyncLinksAsync after SaveChangesAsync

**Implementation Details:**
- Added `ILinkSyncService` to constructor dependencies
- Called `await _linkSyncService.SyncLinksAsync(article.Id, article.Body)` after article is saved
- Link sync happens automatically on every article update
- All wiki links in body are parsed and stored in ArticleLinks table

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ Saving an article with `[[some-guid]]` in body creates row in ArticleLinks table
- ✅ Saving same article with different links updates the table (old removed, new added)
- ✅ Saving article with no links removes any existing links

---

## 🎉 Backend Foundation Complete! (Phases WL-1 through WL-4)

The entire backend infrastructure for wiki links is now in place:
- ✅ Database schema with ArticleLink table
- ✅ Parser to extract links from markdown
- ✅ Sync service to update database
- ✅ Automatic sync on article updates

Next up: API endpoints for frontend features (WL-5 through WL-8)

---

## Phase WL-5: Link DTOs ✅ COMPLETE

**Status:** Complete (December 22, 2025)

**Goal:** Create DTOs for link-related API responses.

**Created:**
- ✅ `Chronicis.Shared/DTOs/LinkDtos.cs` - Contains all link-related DTOs:
  - `LinkSuggestionsResponseDto` & `LinkSuggestionDto` - For autocomplete
  - `BacklinksResponseDto` & `BacklinkDto` - For backlinks panel  
  - `LinkResolutionRequestDto`, `LinkResolutionResponseDto` & `ResolvedLinkDto` - For broken link detection

**Implementation Details:**
- All DTOs in Shared project (accessible from both API and Client)
- DisplayPath for hierarchical context
- Slug for navigation
- ArticleType for filtering/icons
- Snippet optional for backlinks

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ DTOs are in Chronicis.Shared so both API and Client can use them

---

## Phase WL-6: Link Suggestions API Endpoint ✅ COMPLETE

**Status:** Complete (December 22, 2025)

**Goal:** Create API endpoint for autocomplete suggestions.

**Created:**
- ✅ `Chronicis.Api/Functions/LinkSuggestionsFunctions.cs` - Azure Function with `GET /api/worlds/{worldId}/link-suggestions?query={string}`

**Implementation Details:**
- Returns empty list if query < 3 characters
- Gets all articles in world scoped to authenticated user
- Builds display paths by walking up parent chain
- Strips first level from paths (direct child of World)
- Excludes first-level children (returns null path)
- Filters where path starts with query (case-insensitive)
- Sorts alphabetically
- Returns top 10 results
- Uses `ArticlePathInfo` helper class for type safety

**Display Path Logic:**
- Walk up parent chain to build full path
- Strip first level (e.g., "Wiki", "NPCs" containers)
- Example: "World / Wiki / Sword Coast / Waterdeep" → "Sword Coast / Waterdeep"
- First-level children excluded from results

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ Endpoint returns suggestions with DisplayPath stripped of first level
- ✅ Query "Swo" matches "Sword Coast" and "Sword Coast / Waterdeep"
- ✅ Query "Wat" does NOT match "Sword Coast / Waterdeep" (path doesn't start with Wat)
- ✅ Results sorted alphabetically
- ✅ Max 10 results
- ✅ Empty query or < 3 chars returns empty list

---

## Phase WL-7: Backlinks API Endpoint ✅ COMPLETE

**Status:** Complete (December 22, 2025)

**Goal:** Create API endpoint that returns articles linking to a given article.

**Created:**
- ✅ `Chronicis.Api/Functions/BacklinkFunctions.cs` - Azure Function with `GET /api/articles/{articleId}/backlinks`

**Implementation Details:**
- Queries ArticleLinks where TargetArticleId matches
- Includes SourceArticle navigation property
- User scoping (only returns backlinks from user's articles)
- Returns BacklinkDto with ArticleId, Title, DisplayPath, Slug
- Sorted alphabetically by title
- DisplayPath currently uses Title (can enhance with full path later)
- Snippet currently null (can add context extraction later)

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ Endpoint returns articles that link to the specified article
- ✅ Each result includes article ID, title, and slug
- ✅ Results sorted alphabetically

---

## Phase WL-8: Link Resolution API Endpoint ✅ COMPLETE

**Status:** Complete (December 22, 2025)

**Goal:** Create API endpoint to resolve multiple article IDs at once (for rendering and broken link detection).

**Created:**
- ✅ `Chronicis.Api/Functions/LinkResolutionFunctions.cs` - Azure Function with `POST /api/articles/resolve-links`

**Implementation Details:**
- Body: `{ "articleIds": ["guid1", "guid2", ...] }`
- Single database query for all IDs (efficient - no N+1!)
- User scoping (only checks user's articles)
- Returns dictionary mapping each ID to ResolvedLinkDto
- Logs warning for each broken link
- Logs metrics: count of links and duration in ms
- Uses Stopwatch for performance tracking
- Validates request has at least one article ID

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ Existing articles return exists=true with title and slug
- ✅ Non-existent articles return exists=false
- ✅ Single database query for all IDs (not N+1)
- ✅ Logging includes link count and duration

---

## 🎉🎉🎉 BACKEND COMPLETE! (Phases WL-1 through WL-8) 🎉🎉🎉

**ALL backend infrastructure and APIs are now complete:**
- ✅ Database schema with ArticleLink table
- ✅ Link parser (regex-based)
- ✅ Link sync service (auto-updates on save)
- ✅ Link sync integration (automatic)
- ✅ All DTOs defined
- ✅ Autocomplete API (link suggestions)
- ✅ Backlinks API (what links here)
- ✅ Link resolution API (broken link detection)

**The backend is production-ready!** Every endpoint needed for wiki links is implemented, tested, and working.

**Next up:** Frontend implementation (Phases WL-9 through WL-21)
- Client services
- UI components
- TipTap integration
- User experience features

---

## Phase WL-9: Client Link API Service ✅ COMPLETE

**Status:** Complete (December 23, 2025)

**Goal:** Create client-side service to call link APIs.

**Created:**
- ✅ `Chronicis.Client/Services/ILinkApiService.cs` - Interface with 3 methods
- ✅ `Chronicis.Client/Services/LinkApiService.cs` - Implementation using factory-created HttpClient

**Modified:**
- ✅ `Chronicis.Client/Program.cs` - Registered ILinkApiService using "ChronicisApi" named HttpClient

**Implementation Details:**
- GetSuggestionsAsync(worldId, query) - For autocomplete
- GetBacklinksAsync(articleId) - For backlinks panel
- ResolveLinksAsync(articleIds) - For broken link detection
- Uses ChronicisAuthHandler for automatic token attachment
- Error handling with graceful degradation (returns empty collections)
- URI encoding for query parameters
- Comprehensive logging

**Success Criteria:**
- ✅ Solution builds with no errors
- ✅ Service registered in DI
- ✅ Methods call correct endpoints and deserialize responses

---

## Phase WL-10: Wiki Link CSS

**Goal:** Create styles for wiki links in the editor and rendered content.

**Create:**
- `Chronicis.Client/wwwroot/css/chronicis-wiki-links.css`:
  - Style for valid wiki links (beige-gold color, subtle underline, cursor pointer)
  - Style for broken wiki links (red/muted color, strikethrough or dashed underline)
  - Hover state with slight glow (consistent with Chronicis style guide)
  - Style for wiki link in autocomplete dropdown

**Modify:**
- `Chronicis.Client/wwwroot/index.html`:
  - Add link to chronicis-wiki-links.css

**Success Criteria:**
- [ ] Solution builds with no errors
- [ ] CSS file created with appropriate styles
- [ ] CSS referenced in index.html

---

## Phase WL-11: TipTap Wiki Link Extension (JavaScript)

**Goal:** Create TipTap node extension for wiki links.

**Create:**
- `Chronicis.Client/wwwroot/js/wikiLinkExtension.js`:
  - Define a TipTap Node called "wikiLink"
  - Attributes: targetArticleId (string), displayText (string, optional)
  - Renders as: `<span data-type="wiki-link" data-target-id="..." data-display="...">display text</span>`
  - Inline node (not block)
  - Export function to create the extension

**Success Criteria:**
- [ ] JavaScript file created
- [ ] Extension defines proper attributes
- [ ] Extension renders correct HTML structure

---

## Phase WL-12: TipTap Integration - Load Wiki Link Extension

**Goal:** Load the wiki link extension in TipTap initialization.

**Modify:**
- `Chronicis.Client/wwwroot/js/tipTapIntegration.js`:
  - Import/load wikiLinkExtension.js
  - Add wiki link extension to extensions array in initializeTipTapEditor
  - No other changes yet (autocomplete comes later)

**Modify:**
- `Chronicis.Client/wwwroot/index.html`:
  - Add script reference to wikiLinkExtension.js (before tipTapIntegration.js)

**Success Criteria:**
- [ ] Solution builds with no errors
- [ ] TipTap initializes without errors in browser console
- [ ] Wiki link extension is loaded (verify in console or debug)

---

## Phase WL-13: Markdown Conversion - Wiki Links

**Goal:** Update markdown/HTML conversion to handle wiki link syntax.

**Modify:**
- `Chronicis.Client/wwwroot/js/tipTapIntegration.js`:

  In `markdownToHTML`:
  - Detect `[[guid]]` pattern → convert to wiki-link span with just targetId
  - Detect `[[guid|display]]` pattern → convert to wiki-link span with targetId and display
  - Use regex similar to backend parser

  In `htmlToMarkdown`:
  - Detect wiki-link spans → convert back to `[[guid]]` or `[[guid|display]]`
  - Extract targetId from data-target-id attribute
  - Extract display from data-display attribute or inner text

**Success Criteria:**
- [ ] Solution builds with no errors
- [ ] Markdown `[[guid]]` converts to proper span on load
- [ ] Markdown `[[guid|text]]` converts to span with display text
- [ ] Span converts back to markdown syntax on save
- [ ] Round-trip works: markdown → HTML → markdown produces same result

---

## Phase WL-14: Wiki Link Click Navigation

**Goal:** Make wiki links clickable to navigate to target article.

**Modify:**
- `Chronicis.Client/wwwroot/js/tipTapIntegration.js`:
  - Add click handler for wiki-link spans (similar to old hashtag click handler)
  - On click, extract targetArticleId from data attribute
  - Dispatch custom event `wiki-link-clicked` with targetArticleId
  - Prevent default behavior

**Modify:**
- `Chronicis.Client/Components/Articles/ArticleDetail.razor` (or appropriate component):
  - Listen for `wiki-link-clicked` event
  - Look up article slug from resolved links (or call API)
  - Navigate to `/article/{slug}`

**Success Criteria:**
- [ ] Clicking a wiki link navigates to the target article
- [ ] Navigation uses the article's slug URL

---

## Phase WL-15: Backlinks Panel Component

**Goal:** Create a panel showing articles that link to the current article.

**Create:**
- `Chronicis.Client/Components/Articles/BacklinksPanel.razor`:
  - Parameter: Guid ArticleId
  - On parameter change, call ILinkApiService.GetBacklinksAsync
  - Display list of linking articles with title
  - Each item clickable to navigate to that article
  - Show "No backlinks" if empty
  - Show loading state while fetching

**Modify:**
- `Chronicis.Client/Components/Articles/ArticleDetail.razor`:
  - Add BacklinksPanel in the right drawer/panel area
  - Pass current article ID

**Success Criteria:**
- [ ] Solution builds with no errors
- [ ] Panel shows articles that link to current article
- [ ] Clicking a backlink navigates to that article
- [ ] Empty state handled gracefully

---

## Phase WL-16: Autocomplete Component (UI Only)

**Goal:** Create the autocomplete dropdown component without API integration.

**Create:**
- `Chronicis.Client/Components/WikiLinks/WikiLinkAutocomplete.razor`:
  - Parameters: 
    - bool IsVisible
    - string Query
    - List<LinkSuggestionDto> Suggestions
    - EventCallback<LinkSuggestionDto> OnSelect
    - EventCallback<string> OnCreate (article name to create)
    - EventCallback OnCancel
  - Display dropdown with suggestions
  - Keyboard navigation (up/down arrows, Enter to select, Escape to cancel)
  - "Create article" option when no exact match: `+ Create "Query" (in Wiki root)`
  - Style consistent with Chronicis theme

**Success Criteria:**
- [ ] Solution builds with no errors
- [ ] Component renders dropdown with suggestions
- [ ] Keyboard navigation works
- [ ] Create option appears when appropriate
- [ ] Events fire correctly

---

## Phase WL-17: Autocomplete - JS Event Detection

**Goal:** Detect when user types `[[` and has 3+ characters, trigger autocomplete.

**Modify:**
- `Chronicis.Client/wwwroot/js/tipTapIntegration.js`:
  - In editor's onUpdate or via input rule, detect `[[` followed by 3+ non-`]` characters
  - When detected, dispatch custom event `wiki-link-autocomplete-trigger` with:
    - query (text after `[[`)
    - cursor position (for positioning dropdown)
  - When `]]` is typed or cursor moves away, dispatch `wiki-link-autocomplete-cancel`

**Success Criteria:**
- [ ] Typing `[[abc` triggers the autocomplete event
- [ ] Typing `[[ab` (only 2 chars) does NOT trigger
- [ ] Event includes the query text
- [ ] Cancel event fires when appropriate

---

## Phase WL-18: Autocomplete - Blazor Integration

**Goal:** Wire up JS events to show/hide autocomplete and fetch suggestions.

**Modify:**
- `Chronicis.Client/Components/Articles/ArticleDetail.razor` (or create a wrapper component):
  - Listen for `wiki-link-autocomplete-trigger` event
  - When triggered:
    - Show WikiLinkAutocomplete component
    - Call ILinkApiService.GetSuggestionsAsync with current world ID and query
    - Pass suggestions to component
  - Listen for `wiki-link-autocomplete-cancel` event to hide
  - When user selects suggestion:
    - Call JS function to insert wiki link node
    - Hide autocomplete
  - When user selects "create":
    - Create article via API
    - Insert wiki link with new article's ID
    - Hide autocomplete

**Success Criteria:**
- [ ] Typing `[[abc` shows autocomplete dropdown
- [ ] Dropdown populates with API results
- [ ] Selecting a suggestion inserts the link
- [ ] Creating new article works and inserts link
- [ ] Escape or clicking away closes dropdown

---

## Phase WL-19: Insert Wiki Link from Autocomplete

**Goal:** Create JS function to insert a wiki link node at cursor position.

**Modify:**
- `Chronicis.Client/wwwroot/js/tipTapIntegration.js`:
  - Add function `insertWikiLink(editorId, targetArticleId, displayText)`:
    - Get editor instance
    - Delete the `[[query` text that triggered autocomplete
    - Insert wiki-link node with attributes
  - Add function `cancelWikiLinkAutocomplete(editorId)`:
    - Just cleanup, don't insert anything

**Success Criteria:**
- [ ] Selecting from autocomplete replaces `[[query` with proper wiki link
- [ ] Link displays correctly in editor
- [ ] Link saves correctly to markdown format

---

## Phase WL-20: Broken Link Detection & Display

**Goal:** Detect and visually indicate broken links when article loads.

**Modify:**
- `Chronicis.Client/Components/Articles/ArticleDetail.razor`:
  - After loading article, parse body to extract all wiki link target IDs
  - Call ILinkApiService.ResolveLinksAsync with all IDs
  - Pass broken link IDs to JavaScript
  
**Modify:**
- `Chronicis.Client/wwwroot/js/tipTapIntegration.js`:
  - Add function `markBrokenLinks(editorId, brokenIds)`:
    - Find all wiki-link nodes
    - If targetId is in brokenIds, add "broken" class/attribute
  - Update wiki link rendering to check for broken state

**Success Criteria:**
- [ ] Links to deleted articles display with broken styling
- [ ] Links to existing articles display normally
- [ ] Broken state detected on article load

---

## Phase WL-21: Broken Link Recovery Dialog

**Goal:** Show recovery options when user clicks a broken link.

**Create:**
- `Chronicis.Client/Components/WikiLinks/BrokenLinkDialog.razor`:
  - Parameters:
    - bool IsVisible
    - Guid BrokenTargetId
    - EventCallback<Guid> OnRetarget (new target selected)
    - EventCallback OnRemove (remove link entirely)
    - EventCallback OnConvertToText (convert to plain text)
    - EventCallback OnCancel
  - Display options:
    - "Find new target" - opens autocomplete to select new article
    - "Remove link" - deletes the link markup
    - "Convert to plain text" - keeps text but removes link
    - "Cancel"

**Modify:**
- Wiki link click handler:
  - If link is broken, show BrokenLinkDialog instead of navigating
  - Handle each recovery action appropriately

**Success Criteria:**
- [ ] Clicking broken link shows dialog
- [ ] Each option works correctly
- [ ] Dialog styled consistently with app

---

## Summary: Implementation Order

1. **WL-1** through **WL-4**: Database and sync (backend foundation)
2. **WL-5** through **WL-8**: API endpoints
3. **WL-9**: Client API service
4. **WL-10** through **WL-13**: TipTap extension and markdown conversion
5. **WL-14** and **WL-15**: Click navigation and backlinks panel
6. **WL-16** through **WL-19**: Autocomplete feature
7. **WL-20** and **WL-21**: Broken link handling

Each phase can be done independently in sequence. Phases 1-4 must be done before 5-8. Phase 9 can parallel 5-8. Frontend phases (10+) require backend phases complete.

---

## Handoff Instructions for Sonnet

When starting each phase, tell Sonnet:

> "Please complete Phase WL-X of the Wiki Links implementation. The project is at Z:\repos\chronicis. Reference Z:\repos\chronicis\docs\WIKI_LINKS_ARCHITECTURE.md for full design context and Z:\repos\chronicis\docs\WIKI_LINKS_IMPLEMENTATION_PHASES.md for phase details."

After each phase, verify the success criteria before moving to the next.

---

*End of Implementation Phases Document*
