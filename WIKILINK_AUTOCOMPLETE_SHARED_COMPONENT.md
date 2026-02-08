# WikiLinkAutocomplete Shared Component - Implementation Plan

**Date:** February 8, 2026  
**Status:** 🚧 IN PROGRESS

---

## Overview

Create a shared, reusable WikiLinkAutocomplete component that can be used by both ArticleDetail and QuestDrawer to provide consistent `[[wiki link]]` and `[[external resource]]` autocomplete functionality.

---

## Architecture

### Components Created

1. ✅ **IWikiLinkAutocompleteService.cs** - Service interface defining autocomplete state and operations
2. ✅ **WikiLinkAutocompleteService.cs** - Service implementation managing state and API calls
3. ✅ **Program.cs** - Service registration added
4. ⏳ **WikiLinkAutocomplete.razor** - Reusable UI component for autocomplete dropdown
5. ⏳ **QuestDrawer integration** - Update to use shared service and component
6. 🔄 **ArticleDetail refactor** (Future) - Migrate to shared component for consistency

---

## Service Design

### WikiLinkAutocompleteService

**Responsibilities:**
- Manage autocomplete visibility state
- Handle query parsing (internal vs external)
- Fetch suggestions from appropriate API
- Manage selection state (arrow up/down)
- Provide events for UI updates

**Key Methods:**
- `ShowAsync(query, x, y, worldId)` - Show autocomplete at position
- `Hide()` - Hide autocomplete
- `SelectNext()` / `SelectPrevious()` - Navigate suggestions
- `GetSelectedSuggestion()` - Get currently selected item

**Events:**
- `OnShow` - Autocomplete shown
- `OnHide` - Autocomplete hidden
- `OnSuggestionsUpdated` - Suggestions list changed

---

## Component Design (To Be Implemented)

### WikiLinkAutocomplete.razor

**Parameters:**
```csharp
[Parameter] public string EditorId { get; set; } = null!;
[Parameter] public Guid? WorldId { get; set; }
[Parameter] public EventCallback<WikiLinkAutocompleteItem> OnSuggestionSelected { get; set; }
```

**Features:**
- Positioned absolutely at cursor location
- Keyboard navigation (Arrow Up/Down, Enter, Escape)
- Loading indicator
- Empty state messaging
- Category vs item differentiation for external resources
- Tooltip display on hover

**Styling:**
- Match existing `.wiki-link-autocomplete` CSS
- Beige-gold highlights for selected items
- Proper z-index layering
- Scroll support for long lists

---

## Integration Points

### QuestDrawer.razor.cs

**Add JSInvokable Methods:**
```csharp
[JSInvokable]
public Task OnAutocompleteTriggered(string query, double x, double y)
{
    return _autocompleteService.ShowAsync(query, x, y, _currentWorldId);
}

[JSInvokable]
public Task OnAutocompleteHidden()
{
    _autocompleteService.Hide();
    return Task.CompletedTask;
}

[JSInvokable]
public Task OnAutocompleteArrowDown()
{
    _autocompleteService.SelectNext();
    return Task.CompletedTask;
}

[JSInvokable]
public Task OnAutocompleteArrowUp()
{
    _autocompleteService.SelectPrevious();
    return Task.CompletedTask;
}

[JSInvokable]
public async Task OnAutocompleteEnter()
{
    var selected = _autocompleteService.GetSelectedSuggestion();
    if (selected != null)
    {
        await InsertWikiLink(selected);
    }
}
```

**Add Component Reference:**
```razor
<WikiLinkAutocomplete EditorId="quest-update-editor" 
                      WorldId="@_currentWorldId"
                      OnSuggestionSelected="HandleSuggestionSelected" />
```

---

## JavaScript Integration

### questEditor.js

Already updated with:
- ✅ Wiki link extension loading
- ✅ External link extension loading  
- ✅ Editor stored in `window.tipTapEditors`
- ✅ `initializeWikiLinkAutocomplete()` called with dotNetRef

The existing `wikiLinkAutocomplete.js` handles:
- Detecting `[[` typing
- Calling `OnAutocompleteTriggered` with query and position
- Keyboard event handling (Arrow keys, Enter, Escape)
- Calling appropriate callback methods on C# component

---

## Data Flow

```
User Types [[
    ↓
wikiLinkAutocomplete.js detects input
    ↓
Calls dotNetRef.OnAutocompleteTriggered(query, x, y)
    ↓
QuestDrawer/ArticleDetail forwards to WikiLinkAutocompleteService.ShowAsync()
    ↓
Service fetches suggestions from API
    ↓
Service raises OnSuggestionsUpdated event
    ↓
WikiLinkAutocomplete component updates UI
    ↓
User navigates with Arrow keys (JS calls OnAutocompleteArrowDown/Up)
    ↓
Service updates SelectedIndex
    ↓
User presses Enter (JS calls OnAutocompleteEnter)
    ↓
QuestDrawer/ArticleDetail gets selected suggestion
    ↓
Inserts wiki link into editor via JS
```

---

## Remaining Work

### High Priority
1. **Create WikiLinkAutocomplete.razor component**
   - UI markup with proper positioning
   - Loading state, empty state
   - List rendering with selection highlight
   - Event handling for click selection

2. **Update QuestDrawer.razor.cs**
   - Inject IWikiLinkAutocompleteService
   - Add [JSInvokable] callback methods
   - Implement InsertWikiLink method
   - Track current WorldId for queries

3. **Update QuestDrawer.razor markup**
   - Add WikiLinkAutocomplete component reference
   - Wire up OnSuggestionSelected event

4. **Test Integration**
   - Type `[[` in quest editor
   - Verify autocomplete appears
   - Test keyboard navigation
   - Test selection and insertion
   - Test external resource queries (`[[srd/...]]`)

### Medium Priority
5. **Refactor ArticleDetail (Future)**
   - Remove inline autocomplete code
   - Use shared WikiLinkAutocomplete component
   - Migrate to WikiLinkAutocompleteService
   - Ensure backwards compatibility

### Low Priority
6. **Documentation**
   - Component usage examples
   - Service API documentation
   - Integration guide for new editors

---

## Benefits of This Architecture

✅ **Reusability** - One autocomplete implementation for all editors  
✅ **Maintainability** - Single source of truth for autocomplete logic  
✅ **Testability** - Service can be unit tested independently  
✅ **Separation of Concerns** - UI, state, and business logic properly separated  
✅ **Consistency** - Same UX across all editors  
✅ **Extensibility** - Easy to add new editor types or autocomplete sources

---

## Files Modified/Created

### Created
- ✅ `src/Chronicis.Client/Services/IWikiLinkAutocompleteService.cs`
- ✅ `src/Chronicis.Client/Services/WikiLinkAutocompleteService.cs`
- ⏳ `src/Chronicis.Client/Components/Shared/WikiLinkAutocomplete.razor`
- ⏳ `src/Chronicis.Client/Components/Shared/WikiLinkAutocomplete.razor.cs`

### Modified
- ✅ `src/Chronicis.Client/Program.cs` - Service registration
- ✅ `src/Chronicis.Client/wwwroot/js/questEditor.js` - Extension loading
- ⏳ `src/Chronicis.Client/Components/Quests/QuestDrawer.razor` - Component reference
- ⏳ `src/Chronicis.Client/Components/Quests/QuestDrawer.razor.cs` - Service integration

---

## Next Steps

1. Create WikiLinkAutocomplete.razor component
2. Integrate into QuestDrawer
3. Test autocomplete functionality
4. Verify external resource queries work
5. Document usage patterns

---

**Status:** Core service layer complete. UI component and integration pending.  
**Blockers:** None  
**ETA:** Next session
