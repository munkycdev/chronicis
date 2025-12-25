# ArticleDetail.razor Refactoring Plan

**Created:** December 25, 2025  
**Status:** Planned  
**Current Size:** ~1,150 lines  
**Target Size:** ~200 lines (orchestrator) + 5 sub-components

---

## Problem Statement

`ArticleDetail.razor` has grown to ~1,150 lines with too many responsibilities:
- Too large to safely refactor (edits corrupt the file)
- Difficult to test individual features
- Hard to reason about state flow
- Violates Single Responsibility Principle

---

## Current Responsibilities

1. **Article Loading & State** - fetching, caching, breadcrumbs
2. **Title/Icon Editing** - inline editing with auto-save
3. **TipTap Editor Integration** - JS interop, content sync
4. **Save Operations** - auto-save, manual save, slug updates
5. **Delete Operations** - confirmation, cascade handling
6. **Auto-Link Feature** - scanning and inserting wiki links
7. **Link Autocomplete** - suggestions popup during editing
8. **Metadata Drawer** - toggle, display, link panels
9. **Keyboard Shortcuts** - Ctrl+N handling
10. **Empty State** - "no article selected" UI

---

## Proposed Component Breakdown

### 1. ArticleDetail.razor (Orchestrator)
**Target Size:** ~200 lines  
**Responsibility:** State management, routing, coordination

**Keeps:**
- Article loading and state (`_article`, `_isLoading`)
- Route parameter handling
- Coordination between sub-components
- Empty state rendering

**Parameters to pass down:**
- `ArticleDto` to child components
- Event callbacks for state changes

---

### 2. ArticleHeader.razor
**Target Size:** ~100 lines  
**Responsibility:** Top section of article display

**Includes:**
- Breadcrumb navigation
- Title field (inline editable)
- Icon picker integration
- Save status indicator

**Parameters:**
```csharp
[Parameter] public ArticleDto Article { get; set; }
[Parameter] public List<BreadcrumbItem> Breadcrumbs { get; set; }
[Parameter] public bool IsSaving { get; set; }
[Parameter] public bool HasUnsavedChanges { get; set; }
[Parameter] public string? LastSaveTime { get; set; }
[Parameter] public EventCallback<string> OnTitleChanged { get; set; }
[Parameter] public EventCallback<string?> OnIconChanged { get; set; }
[Parameter] public EventCallback OnSaveRequested { get; set; }
```

---

### 3. ArticleEditor.razor
**Target Size:** ~250 lines  
**Responsibility:** TipTap WYSIWYG editor integration

**Includes:**
- TipTap initialization and JS interop
- Content change detection
- Auto-save timer management
- Editor toolbar (if any)

**Parameters:**
```csharp
[Parameter] public string Body { get; set; }
[Parameter] public Guid ArticleId { get; set; }
[Parameter] public Guid? WorldId { get; set; }
[Parameter] public EventCallback<string> OnContentChanged { get; set; }
[Parameter] public EventCallback OnSaveRequested { get; set; }
```

**Note:** This component owns the `_autoSaveTimer` and debouncing logic.

---

### 4. ArticleMetadataDrawer.razor
**Target Size:** ~150 lines  
**Responsibility:** Right-side metadata panel

**Includes:**
- Drawer open/close state
- Outgoing links panel
- Backlinks panel
- Article info (created, modified dates)
- AI Summary section reference

**Parameters:**
```csharp
[Parameter] public ArticleDto Article { get; set; }
[Parameter] public bool IsOpen { get; set; }
[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
```

---

### 5. ArticleActions.razor
**Target Size:** ~100 lines  
**Responsibility:** Action buttons and their logic

**Includes:**
- Delete button with confirmation dialog
- Auto-link button and processing
- Any future action buttons

**Parameters:**
```csharp
[Parameter] public ArticleDto Article { get; set; }
[Parameter] public EventCallback OnDeleted { get; set; }
[Parameter] public EventCallback<string> OnBodyUpdated { get; set; }
```

---

### 6. LinkAutocomplete.razor
**Target Size:** ~200 lines  
**Responsibility:** Wiki link suggestion popup

**Includes:**
- Suggestion fetching and caching
- Keyboard navigation (up/down/enter/escape)
- Popup positioning
- Selection handling

**Parameters:**
```csharp
[Parameter] public Guid WorldId { get; set; }
[Parameter] public EventCallback<LinkSuggestionDto> OnLinkSelected { get; set; }
[Parameter] public EventCallback OnDismissed { get; set; }
```

**Note:** May need JS interop for positioning relative to cursor.

---

## Implementation Order

Recommended order based on dependencies and isolation:

### Phase 1: ArticleHeader
- **Why first:** Self-contained, no JS interop, reuses existing `SaveStatusIndicator`
- **Risk:** Low
- **Test:** Title editing, icon changes, breadcrumb navigation

### Phase 2: ArticleMetadataDrawer
- **Why second:** Self-contained drawer, existing child components (link panels)
- **Risk:** Low
- **Test:** Drawer toggle, links display

### Phase 3: ArticleActions
- **Why third:** Isolated actions, clear boundaries
- **Risk:** Low-Medium (delete has side effects)
- **Test:** Delete flow, auto-link flow

### Phase 4: ArticleEditor
- **Why fourth:** Complex JS interop, but well-defined interface
- **Risk:** Medium (TipTap integration)
- **Test:** Content editing, auto-save, manual save

### Phase 5: LinkAutocomplete
- **Why last:** Most complex, deeply integrated with editor
- **Risk:** Medium-High (keyboard handling, positioning)
- **Test:** Suggestion popup, keyboard navigation, selection

---

## Migration Strategy

For each component extraction:

1. **Create new component file** with parameters defined
2. **Copy relevant markup** from ArticleDetail.razor
3. **Copy relevant code** (methods, fields)
4. **Add component reference** in ArticleDetail.razor
5. **Wire up parameters and callbacks**
6. **Remove old code** from ArticleDetail.razor
7. **Build and test**
8. **Commit**

### State Management Approach

Keep article state in `ArticleDetail.razor` (orchestrator):
- `_article` - the article being edited
- `_isLoading` - loading state
- `_isSaving` - save in progress
- `_hasUnsavedChanges` - dirty flag

Child components receive state via parameters and communicate back via `EventCallback`.

---

## Code Patterns to Apply

### Null-Safe API Calls
All API calls should use the new extension method pattern:
```csharp
var updated = await ArticleApi.UpdateArticleAsync(_article.Id, updateDto);
if (updated == null)
{
    Snackbar.Add("Failed to save", Severity.Error);
    return;
}
```

### Consistent Save Status
Use `SaveStatusIndicator` component (already created in Tier 2).

### Event Callbacks
Use `EventCallback<T>` for parent-child communication:
```csharp
// Child raises event
await OnContentChanged.InvokeAsync(newContent);

// Parent handles in ArticleDetail.razor
<ArticleEditor OnContentChanged="HandleContentChanged" />
```

---

## Success Criteria

- [ ] ArticleDetail.razor reduced to ~200 lines
- [ ] Each sub-component is self-contained and testable
- [ ] All existing functionality preserved
- [ ] Build succeeds with no new warnings
- [ ] Manual testing confirms all features work
- [ ] Code is easier to understand and modify

---

## Session Guide

When starting a new session to work on this:

```
I'm continuing the ArticleDetail.razor decomposition. 
The project is at Z:\repos\chronicis.

Current status: [which phases are complete]
Next step: Extract [ComponentName].razor

The full plan is in docs/ArticleDetail_Refactoring_Plan.md
```

---

## Related Files

- `src/Chronicis.Client/Components/Articles/ArticleDetail.razor` - Source file
- `src/Chronicis.Client/Components/Shared/SaveStatusIndicator.razor` - Reusable component
- `src/Chronicis.Client/Components/Shared/DetailPageHeader.razor` - Pattern reference
- `src/Chronicis.Client/Services/HttpClientExtensions.cs` - API call patterns

---

## Notes

- Created as part of Christmas 2025 refactoring session
- Builds on Tier 1-3 refactoring (shared components, HttpClient extensions)
- ArticleApiService interface already updated to return nullable types
- Some callers still need null-check updates (will be done during extraction)
