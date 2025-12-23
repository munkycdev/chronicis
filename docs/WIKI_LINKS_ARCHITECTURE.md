# Wiki Links - Technical Architecture

**Version:** 1.0  
**Date:** December 22, 2025  
**Status:** Approved for Implementation

---

## Overview

Wiki links replace the previous hashtag system with a more intuitive `[[Article Name]]` syntax, similar to Obsidian, Notion, and Wikipedia. Links are stored with target article GUIDs for stability across renames and moves.

---

## User-Facing Behavior

### Syntax

- `[[Article Path]]` - displays article title, links to article
- `[[Article Path|Display Text]]` - displays custom text, links to article

### Autocomplete

| Property | Behavior |
|----------|----------|
| **Trigger** | After 3 characters typed following `[[` |
| **Scope** | Current World only |
| **Path display** | First level under World stripped (Wiki, NPCs, custom categories all invisible) |
| **Filtering** | Path (after stripping) starts with typed text |
| **Sort** | Alphabetical |
| **Max results** | 10 |
| **Article types** | All types, no preference |

**Example:** Given hierarchy `World / Wiki / Sword Coast / Waterdeep`:
- Autocomplete displays: `Sword Coast / Waterdeep`
- `[[Swo` matches it
- `[[Wat` does NOT match (path starts with "Sword Coast", not "Waterdeep")

### New Article Creation

- When no match found, autocomplete shows: `+ Create "ArticleName" (in Wiki root)`
- Creates article as direct child of Wiki container
- Link inserted with new article's Guid
- No confirmation dialog
- UX text is configurable for future iteration

### Broken Links

When a linked article is deleted:
- Link displays with visual indicator (red/strikethrough/icon)
- On click, shows recovery dialog:
  - "This article no longer exists."
  - Options: [Find new target] [Remove link] [Convert to plain text]

---

## Data Model

### New Entity: `ArticleLink`

```csharp
public class ArticleLink
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// The article containing the link.
    /// </summary>
    public Guid SourceArticleId { get; set; }
    
    /// <summary>
    /// The article being linked to.
    /// </summary>
    public Guid TargetArticleId { get; set; }
    
    /// <summary>
    /// Custom display text. Null means use target article's title.
    /// </summary>
    public string? DisplayText { get; set; }
    
    /// <summary>
    /// Position in the source article body (character offset).
    /// Used for ordering in backlinks display.
    /// </summary>
    public int Position { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Article SourceArticle { get; set; } = null!;
    public Article TargetArticle { get; set; } = null!;
}
```

### Database Configuration

```csharp
modelBuilder.Entity<ArticleLink>(entity =>
{
    entity.HasKey(al => al.Id);
    
    entity.HasOne(al => al.SourceArticle)
        .WithMany(a => a.OutgoingLinks)
        .HasForeignKey(al => al.SourceArticleId)
        .OnDelete(DeleteBehavior.Cascade);
    
    entity.HasOne(al => al.TargetArticle)
        .WithMany(a => a.IncomingLinks)
        .HasForeignKey(al => al.TargetArticleId)
        .OnDelete(DeleteBehavior.Cascade);
    
    // Prevent duplicate links to same target from same source
    entity.HasIndex(al => new { al.SourceArticleId, al.TargetArticleId, al.Position })
        .IsUnique();
});
```

### Article Entity Updates

Add navigation properties to `Article.cs`:

```csharp
/// <summary>
/// Links from this article to other articles.
/// </summary>
public ICollection<ArticleLink> OutgoingLinks { get; set; } = new List<ArticleLink>();

/// <summary>
/// Links from other articles to this article (backlinks).
/// </summary>
public ICollection<ArticleLink> IncomingLinks { get; set; } = new List<ArticleLink>();
```

---

## Body Storage Format

Links are stored in markdown as: `[[guid|Display Text]]`

- If display text equals target title: `[[guid]]` (display text omitted)
- Example: `[[a1b2c3d4-e5f6-7890-abcd-ef1234567890|the City of Splendors]]`

**Rationale:** Naive format is simple, human-inspectable, and easy to migrate if we later want semantic HTML. Server-side sync parses this format.

---

## API Endpoints

### Link Suggestions (Autocomplete)

```
GET /api/worlds/{worldId}/link-suggestions?query={string}
```

**Response:**
```json
{
  "suggestions": [
    {
      "articleId": "guid",
      "title": "Waterdeep",
      "displayPath": "Sword Coast / Waterdeep",
      "articleType": "WikiArticle"
    }
  ]
}
```

**Logic:**
1. Get all articles in World
2. Build display paths (strip first level under World)
3. Filter where display path starts with query (case-insensitive)
4. Sort alphabetically
5. Take first 10

### Backlinks

```
GET /api/articles/{id}/backlinks
```

**Response:**
```json
{
  "backlinks": [
    {
      "articleId": "guid",
      "title": "Session 5",
      "displayPath": "Act 1 / Session 5",
      "snippet": "...visited [[Waterdeep]] for the first time..."
    }
  ]
}
```

### Create Article (for new link targets)

Uses existing article creation endpoint. Client needs to:
1. Find Wiki container ID for current World
2. Create article with `ParentId` = Wiki container ID

### Link Resolution (for rendering)

```
POST /api/articles/resolve-links
Body: { "articleIds": ["guid1", "guid2", ...] }
```

**Response:**
```json
{
  "articles": {
    "guid1": { "exists": true, "title": "Waterdeep", "slug": "waterdeep" },
    "guid2": { "exists": false }
  }
}
```

Used on article load to eagerly resolve all links and identify broken ones.

---

## Link Sync Strategy

**Server-side parsing on article save.**

When `UpdateArticle` is called:
1. Parse body for `[[guid]]` and `[[guid|display]]` patterns
2. Extract list of (targetArticleId, displayText, position) tuples
3. Delete existing `ArticleLink` rows for this source article
4. Insert new `ArticleLink` rows

**Service: `ILinkSyncService`**

```csharp
public interface ILinkSyncService
{
    /// <summary>
    /// Parses article body and syncs ArticleLink table.
    /// </summary>
    Task SyncLinksAsync(Guid articleId, string body);
}
```

**Parser: `ILinkParser`**

```csharp
public interface ILinkParser
{
    /// <summary>
    /// Extracts wiki links from markdown body.
    /// </summary>
    IEnumerable<ParsedLink> ParseLinks(string body);
}

public record ParsedLink(Guid TargetArticleId, string? DisplayText, int Position);
```

---

## Frontend Architecture

### TipTap Extension: WikiLinkNode

**Type:** Node (not Mark)

**Attributes:**
- `targetArticleId` (string - Guid)
- `displayText` (string, optional)

**Rendering:**
- Displays as inline clickable element
- Shows display text, or fetches title if not provided
- Broken links get distinct styling

**Behavior:**
- Click: Navigate to target article
- Click (broken): Show recovery dialog

### WikiLinkAutocomplete Component

**Blazor component** positioned near cursor when `[[` detected.

**State:**
- `query` - text typed after `[[`
- `suggestions` - from API
- `selectedIndex` - keyboard navigation
- `isCreating` - show "create" option

**Events:**
- `OnSelect(ArticleSuggestion)` - insert link
- `OnCreate(string articleName)` - create article, then insert link
- `OnCancel` - close without action

### JavaScript ↔ Blazor Communication

**JS → Blazor:**
- `wiki-link-trigger` event when user types `[[` and 3+ characters
- Includes: cursor position, query text

**Blazor → JS:**
- `insertWikiLink(editorId, targetArticleId, displayText)` - inserts node
- `closeAutocomplete(editorId)` - cleanup

### Markdown ↔ HTML Conversion

**markdownToHTML:**
- Detect `[[guid]]` and `[[guid|display]]` patterns
- Convert to: `<span data-type="wiki-link" data-target-id="guid" data-display="display">display</span>`

**htmlToMarkdown:**
- Detect wiki-link spans
- Convert back to: `[[guid|display]]` or `[[guid]]`

---

## Backlinks Panel

**Component:** `BacklinksPanel.razor`

**Location:** Right drawer/panel (same as old implementation)

**Display:**
- List of articles linking to current article
- Each shows: title, path, snippet with link context
- Click navigates to source article

**Data:** Fetched from `GET /api/articles/{id}/backlinks`

---

## Metrics & Monitoring

### Application Insights Logging

**On article load (link resolution):**
```csharp
_logger.LogInformation(
    "Resolved {LinkCount} links for article {ArticleId} in {DurationMs}ms",
    linkCount, articleId, duration.TotalMilliseconds);
```

**Thresholds to watch:**
- Articles with 50+ links
- Resolution taking >100ms
- These would signal need to consider lazy loading

**On broken link encountered:**
```csharp
_logger.LogWarning(
    "Broken link in article {SourceArticleId} to deleted article {TargetArticleId}",
    sourceId, targetId);
```

---

## Implementation Phases

### Phase A: Data Model & Backend
1. Create `ArticleLink` entity and migration
2. Add navigation properties to `Article`
3. Implement `ILinkParser`
4. Implement `ILinkSyncService`
5. Add sync call to `UpdateArticle`
6. Implement link suggestions endpoint
7. Implement backlinks endpoint
8. Implement link resolution endpoint

### Phase B: Frontend - Basic Links
1. TipTap WikiLinkNode extension
2. Markdown ↔ HTML conversion updates
3. Link click navigation
4. Basic styling

### Phase C: Frontend - Autocomplete
1. WikiLinkAutocomplete component
2. JS ↔ Blazor event communication
3. API integration
4. Keyboard navigation
5. Create new article flow

### Phase D: Frontend - Backlinks & Polish
1. BacklinksPanel component
2. Broken link detection and styling
3. Broken link recovery dialog
4. Metrics logging

---

## Open Items (Pinned for Later)

1. **Rename UX:** When article is renamed, links still work (Guid-based), but display text may be stale if it was auto-generated. Consider: update display text on target rename? Show "title changed" indicator?

2. **Move UX:** Same consideration - paths in autocomplete will change, but existing links remain valid.

3. **Lazy loading:** If metrics show performance issues with eager link resolution, implement lazy loading (resolve on hover/click only).

4. **Create location iteration:** Current design creates in Wiki root. May want to revisit after real usage.

---

## File Checklist (for implementation)

### New Files - API
- [ ] `Models/ArticleLink.cs`
- [ ] `Services/ILinkParser.cs`
- [ ] `Services/LinkParser.cs`
- [ ] `Services/ILinkSyncService.cs`
- [ ] `Services/LinkSyncService.cs`
- [ ] `Functions/LinkSuggestionsFunctions.cs`
- [ ] `Functions/BacklinkFunctions.cs`
- [ ] `Functions/LinkResolutionFunctions.cs`
- [ ] `DTOs/LinkSuggestionDto.cs`
- [ ] `DTOs/BacklinkDto.cs`
- [ ] `DTOs/LinkResolutionDto.cs`

### Modified Files - API
- [ ] `Data/ChronicisDbContext.cs` - Add ArticleLink config
- [ ] `Models/Article.cs` - Add navigation properties
- [ ] `Functions/UpdateArticle.cs` - Call link sync
- [ ] `Program.cs` - Register new services

### New Files - Client
- [ ] `Components/WikiLinks/WikiLinkAutocomplete.razor`
- [ ] `Components/WikiLinks/BrokenLinkDialog.razor`
- [ ] `Components/Articles/BacklinksPanel.razor`
- [ ] `Services/ILinkApiService.cs`
- [ ] `Services/LinkApiService.cs`
- [ ] `wwwroot/js/wikiLinkExtension.js`
- [ ] `wwwroot/css/chronicis-wiki-links.css`

### Modified Files - Client
- [ ] `wwwroot/js/tipTapIntegration.js` - Load wiki link extension, conversion
- [ ] `wwwroot/index.html` - Add CSS reference
- [ ] `Program.cs` - Register LinkApiService
- [ ] `Components/Articles/ArticleDetail.razor` - Add BacklinksPanel

### New Files - Shared
- [ ] `DTOs/LinkSuggestionDto.cs`
- [ ] `DTOs/BacklinkDto.cs`
- [ ] `DTOs/LinkResolutionDto.cs`

---

*End of Architecture Document*
