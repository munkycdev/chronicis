# Hashtag System Removal Task

**Project:** Chronicis (Z:\repos\chronicis)
**Task:** Complete removal of hashtag functionality
**Model:** Claude Sonnet is appropriate for this task

## Context

Chronicis is a Blazor WASM + Azure Functions application. The hashtag system is being removed to make way for a wiki-style `[[link]]` implementation. No data migration is needed - clean removal only.

## Instructions

Please remove all hashtag-related functionality from the Chronicis codebase. This includes database entities, API functions, services, client components, CSS, and JavaScript.

**Important:** After completing all deletions and modifications, create an EF Core migration to drop the Hashtags and ArticleHashtags tables.

---

## FILES TO DELETE (26 files)

Delete these files entirely:

### API - Functions
1. `src/Chronicis.Api/Functions/HashtagFunctions.cs`
2. `src/Chronicis.Api/Functions/BacklinkFunctions.cs`
3. `src/Chronicis.Api/Functions/AutoHashtagFunction.cs`

### API - Services
4. `src/Chronicis.Api/Services/HashtagParser.cs`
5. `src/Chronicis.Api/Services/IHashtagParser.cs`
6. `src/Chronicis.Api/Services/HashtagSyncService.cs`
7. `src/Chronicis.Api/Services/IHashtagSyncService.cs`
8. `src/Chronicis.Api/Services/AutoHashtagService.cs`
9. `src/Chronicis.Api/Services/IAutoHashtagService.cs`

### Client - Components
10. `src/Chronicis.Client/Components/Hashtags/HashtagLinkDialog.razor`
11. `src/Chronicis.Client/Components/Articles/ArticleHashtagsPanel.razor`
12. `src/Chronicis.Client/Components/Articles/BacklinksPanel.razor`
13. `src/Chronicis.Client/Pages/Tools/AutoHashtag.razor`

### Client - Services
14. `src/Chronicis.Client/Services/HashtagApiService.cs`
15. `src/Chronicis.Client/Services/IHashtagApiService.cs`
16. `src/Chronicis.Client/Services/AutoHashtagApiService.cs`
17. `src/Chronicis.Client/Services/IAutoHashtagApiService.cs`

### Client - Assets
18. `src/Chronicis.Client/wwwroot/css/chronicis-hashtags.css`
19. `src/Chronicis.Client/wwwroot/css/chronicis-backlinks.css`
20. `src/Chronicis.Client/wwwroot/js/tipTapHashtagExtension.js`

### Shared - Models
21. `src/Chronicis.Shared/Models/Hashtag.cs`
22. `src/Chronicis.Shared/Models/ArticleHashtag.cs`

### Shared - DTOs
23. `src/Chronicis.Shared/DTOs/HashtagDto.cs`
24. `src/Chronicis.Shared/DTOs/HashtagPreviewDto.cs`
25. `src/Chronicis.Shared/DTOs/BacklinkDto.cs`
26. `src/Chronicis.Shared/DTOs/AutoHashtagDtos.cs`

### Directories to Delete (if empty after file removal)
27. `src/Chronicis.Client/Components/Hashtags/` (entire directory)

---

## FILES TO MODIFY (6 files)

### 1. `src/Chronicis.Api/Data/ChronicisDbContext.cs`

Remove:
- `public DbSet<Hashtag> Hashtags { get; set; } = null!;`
- `public DbSet<ArticleHashtag> ArticleHashtags { get; set; } = null!;`
- The entire `ConfigureHashtag()` method
- The entire `ConfigureArticleHashtag()` method
- The calls to `ConfigureHashtag(modelBuilder);` and `ConfigureArticleHashtag(modelBuilder);` in `OnModelCreating()`
- Remove the comment `// ===== Hashtag System =====`

### 2. `src/Chronicis.Api/Program.cs`

Remove these service registrations:
```csharp
services.AddScoped<IHashtagParser, HashtagParser>();
services.AddScoped<IHashtagSyncService, HashtagSyncService>();
services.AddScoped<IAutoHashtagService, AutoHashtagService>();
```

### 3. `src/Chronicis.Client/Program.cs`

Remove these service registrations:
```csharp
builder.Services.AddScoped<IHashtagApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<HashtagApiService>>();
    return new HashtagApiService(factory.CreateClient("ChronicisApi"), logger);
});

builder.Services.AddScoped<IAutoHashtagApiService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<AutoHashtagApiService>>();
    return new AutoHashtagApiService(factory.CreateClient("ChronicisApi"), logger);
});
```

Also remove any related `using` statements that become unused.

### 4. `src/Chronicis.Shared/Models/Article.cs`

Remove the navigation property:
```csharp
/// <summary>
/// Hashtags associated with this article.
/// </summary>
public ICollection<ArticleHashtag> ArticleHashtags { get; set; } = new List<ArticleHashtag>();
```

### 5. `src/Chronicis.Client/wwwroot/index.html`

Remove these CSS references:
```html
<link href="/css/chronicis-backlinks.css" rel="stylesheet" />
<link href="/css/chronicis-hashtags.css" rel="stylesheet" />
```

### 6. `src/Chronicis.Client/wwwroot/js/tipTapIntegration.js`

This file needs significant cleanup:

**Remove:**
- The hashtag extension import/loading in `initializeTipTapEditor()`
- The `setupHashtagClickHandler()` function entirely
- The `setupHashtagHoverHandler()` function entirely
- The `showHashtagTooltip()` function entirely
- The `createTooltip()` function entirely
- The `hideHashtagTooltip()` function entirely
- The calls to `setupHashtagClickHandler()` and `setupHashtagHoverHandler()` in `initializeTipTapEditor()`
- Global variables: `window.activeTooltip` and `window.tooltipHideTimeout`

**Modify `markdownToHTML()`:**
Remove the hashtag conversion regex:
```javascript
// Remove this block:
html = html.replace(
    /#(\w+)/g,
    '<span data-type="hashtag" class="chronicis-hashtag" data-hashtag-name="$1" data-linked="false" title="Hashtag">#$1</span>'
);
```

**Modify `htmlToMarkdown()`:**
Remove all hashtag-related regex patterns:
```javascript
// Remove these lines:
markdown = markdown.replace(
    /<span[^>]*data-type="hashtag"[^>]*data-hashtag-name="([^"]*)"[^>]*>.*?<\/span>/gi,
    '#$1'
);
markdown = markdown.replace(/<span[^>]*data-type="hashtag"[^>]*>(#\w+)<\/span>/gi, '$1');
markdown = markdown.replace(/<span[^>]*class="chronicis-hashtag"[^>]*>(#\w+)<\/span>/gi, '$1');
```

**Update the console.log at the end:**
Change from `'✅ TipTap integration script loaded (Phase 7.3)'` to `'✅ TipTap integration script loaded'`

---

## SEARCH FOR ADDITIONAL REFERENCES

After completing the above, search the codebase for any remaining references:

```powershell
# Run from Z:\repos\chronicis
Get-ChildItem -Recurse -Include *.cs,*.razor,*.js,*.css,*.html | Select-String -Pattern "hashtag|Hashtag|backlink|Backlink" | Select-Object Path, LineNumber, Line
```

Fix any additional references found.

---

## CREATE DATABASE MIGRATION

After all code changes are complete and the solution builds cleanly:

```powershell
cd Z:\repos\chronicis\src\Chronicis.Api
dotnet ef migrations add RemoveHashtagTables
```

This will generate a migration that drops the `Hashtags` and `ArticleHashtags` tables.

---

## VERIFICATION CHECKLIST

Before declaring complete:

1. [ ] All 26 files deleted
2. [ ] All 6 files modified correctly
3. [ ] Solution builds with no errors (`dotnet build` from solution root)
4. [ ] No warnings related to hashtags
5. [ ] Search for "hashtag" returns no results in source files
6. [ ] Migration created successfully
7. [ ] Hashtags directory deleted (if empty)

---

## NOTES

- The `escapeHtml()` function in tipTapIntegration.js can be kept - it's a general utility
- Don't touch any AI Summary functionality - that stays
- Don't touch the Search functionality - that stays (it may have searched hashtags but the core search remains)
- If you find components that reference BacklinksPanel or similar, remove those references

Good luck! This is a straightforward deletion task.
