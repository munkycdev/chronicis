# Phase 7D-2C: ArticleDetail ViewModel Decomposition Plan

**Created:** February 16, 2026  
**Status:** Planning phase - further decomposition of ArticleDetail ViewModels

---

## Current State

### What We've Built (Phase 7D-2A) ✅

**Core CRUD & State Management:**
- `ArticleLoadingState` - Loading, error, success messages
- `ArticleEditState` - Edit mode, title/body/date fields
- `ArticleOperations` - Save, delete, create child operations
- `ArticleDetailViewModel` - Coordinator for the above

**Test Coverage:** 60 tests passing (33 facade + 27 viewmodel)

### The Gap

ArticleDetail component (1,289 lines) has specialized responsibilities our current ViewModel doesn't handle:

1. **Wiki Link Autocomplete** (~150 lines)
   - Autocomplete popup positioning
   - Internal/external query parsing
   - Suggestion loading and filtering
   - Keyboard navigation (arrow keys, enter)
   - Selection and creation of new articles

2. **External Link Preview** (~100 lines)
   - Preview drawer open/close
   - Content loading and caching
   - Error handling
   - Source/ID management

3. **Image Upload** (~100 lines)
   - Drag/drop handling
   - Paste handling
   - Upload progress
   - Image proxy URL generation
   - Document confirmation

4. **Editor Integration** (~150 lines)
   - TipTap initialization
   - Content synchronization
   - Auto-save timer
   - Editor state tracking
   - JSInterop coordination

5. **UI Coordination** (~50 lines)
   - Breadcrumb management
   - Page title updates
   - Icon management
   - Metadata drawer toggle
   - Focus management

---

## Proposed Architecture

### Option A: Specialized Feature ViewModels (RECOMMENDED)

Create focused ViewModels for each specialized feature:

```
ArticleDetail/
├── Core (EXISTING)
│   ├── ArticleLoadingState.cs
│   ├── ArticleEditState.cs
│   ├── ArticleOperations.cs
│   └── ArticleDetailViewModel.cs (coordinator)
│
├── Autocomplete (NEW)
│   ├── WikiLinkAutocompleteState.cs
│   ├── WikiLinkAutocompleteViewModel.cs
│   └── WikiLinkAutocompleteViewModelTests.cs
│
├── Preview (NEW)
│   ├── ExternalLinkPreviewState.cs
│   ├── ExternalLinkPreviewViewModel.cs
│   └── ExternalLinkPreviewViewModelTests.cs
│
├── Images (NEW)
│   ├── ImageUploadState.cs
│   ├── ImageUploadViewModel.cs
│   └── ImageUploadViewModelTests.cs
│
└── Editor (NEW)
    ├── EditorState.cs
    ├── EditorViewModel.cs
    └── EditorViewModelTests.cs
```

**Pros:**
- Maintains Single Responsibility Principle
- Each ViewModel is independently testable
- Easy to understand and maintain
- Can be developed incrementally
- Follows the pattern we established with Core ViewModels

**Cons:**
- More files to manage
- Component needs to coordinate multiple ViewModels

### Option B: Expand Existing ViewModel

Add all features to `ArticleDetailViewModel`:

**Pros:**
- Single ViewModel, simpler component injection

**Cons:**
- Would create a massive 500+ line ViewModel
- Violates Single Responsibility Principle
- Harder to test
- Harder to understand and maintain

### Option C: Keep Features in Component

Don't extract specialized features to ViewModels:

**Pros:**
- Less refactoring work upfront

**Cons:**
- Component remains complex and hard to test
- Doesn't achieve our testability goals
- Mixed concerns in component

---

## Recommended Approach: Option A

Create specialized ViewModels incrementally, one at a time.

### Phase 7D-2C: WikiLinkAutocompleteViewModel

**Purpose:** Handle autocomplete popup behavior for wiki links

**Responsibilities:**
- Manage autocomplete show/hide state
- Parse internal vs external queries
- Load suggestions from appropriate source
- Handle keyboard navigation
- Handle selection/creation

**State Properties:**
```csharp
bool ShowAutocomplete { get; }
bool IsLoading { get; }
List<WikiLinkAutocompleteItem> Suggestions { get; }
int SelectedIndex { get; }
(double X, double Y) Position { get; }
string Query { get; }
bool IsExternalQuery { get; }
string? ExternalSourceKey { get; }
```

**Methods:**
```csharp
Task TriggerAutocompleteAsync(string query, double x, double y);
void Hide();
void SelectNext();
void SelectPrevious();
Task<bool> SelectCurrentAsync();
Task<bool> CreateNewArticleAsync(string name);
```

**Dependencies:**
- IArticleDetailFacade (for GetSuggestions, CreateFromWikiLink)

**Tests to Write:** ~15-20 tests
- Trigger with internal query
- Trigger with external query  
- Parse external query format
- Load suggestions
- Keyboard navigation
- Selection
- Creation
- Hide

**Component Changes:**
- Inject WikiLinkAutocompleteViewModel
- Replace autocomplete state fields with ViewModel
- Replace autocomplete methods with ViewModel calls
- Update JSInvokable callbacks to use ViewModel

**Estimated Lines:**
- State: ~40 lines
- ViewModel: ~120 lines
- Tests: ~180 lines
- **Total: ~340 lines**

---

### Phase 7D-2D: ExternalLinkPreviewViewModel

**Purpose:** Manage external link preview drawer

**Responsibilities:**
- Open/close drawer
- Load external content
- Cache content
- Handle errors

**State Properties:**
```csharp
bool IsOpen { get; }
bool IsLoading { get; }
string? ErrorMessage { get; }
ExternalLinkContentDto? Content { get; }
string? Source { get; }
string? Id { get; }
string? Title { get; }
```

**Methods:**
```csharp
Task OpenAsync(string source, string id, string title);
void Close();
```

**Dependencies:**
- IArticleDetailFacade (for GetExternalLinkContent)

**Tests to Write:** ~10 tests
- Open with valid source/id
- Load content successfully
- Handle load errors
- Cache content
- Close drawer

**Estimated Lines:**
- State: ~30 lines
- ViewModel: ~80 lines
- Tests: ~120 lines
- **Total: ~230 lines**

---

### Phase 7D-2E: ImageUploadViewModel

**Purpose:** Handle image upload operations

**Responsibilities:**
- Request upload URL
- Track upload progress
- Confirm upload
- Generate proxy URLs
- Resolve image URLs

**State Properties:**
```csharp
bool IsUploading { get; }
string? CurrentFileName { get; }
string? ErrorMessage { get; }
```

**Methods:**
```csharp
Task<object?> RequestUploadAsync(string fileName, string contentType, long fileSize);
Task ConfirmUploadAsync(string documentId);
string GetProxyUrl(string documentId);
Task<string?> ResolveUrlAsync(string documentId);
void NotifyUploadStarted(string fileName);
void NotifyUploadError(string message);
```

**Dependencies:**
- IArticleDetailFacade (for world API operations)
- ISnackbar (for notifications)

**Tests to Write:** ~12 tests
- Request upload
- Confirm upload
- Get proxy URL
- Resolve URL
- Handle errors
- Notifications

**Estimated Lines:**
- State: ~25 lines
- ViewModel: ~90 lines
- Tests: ~150 lines
- **Total: ~265 lines**

---

### Phase 7D-2F: EditorViewModel

**Purpose:** Manage TipTap editor lifecycle and state

**Responsibilities:**
- Initialize editor
- Track editor content
- Handle content updates
- Manage auto-save timer
- Coordinate editor destruction

**State Properties:**
```csharp
bool IsInitialized { get; }
string Content { get; }
bool HasUnsavedChanges { get; }
```

**Methods:**
```csharp
Task InitializeAsync(string editorId, string initialContent);
void OnContentUpdate(string markdown);
Task DestroyAsync(string editorId);
void StartAutoSave(Func<Task> saveAction);
void StopAutoSave();
```

**Dependencies:**
- IJSRuntime (for editor operations)
- IArticleDetailFacade (for various operations)

**Tests to Write:** ~15 tests
- Initialize editor
- Update content
- Track unsaved changes
- Auto-save timer
- Destroy editor

**Estimated Lines:**
- State: ~30 lines
- ViewModel: ~100 lines
- Tests: ~180 lines
- **Total: ~310 lines**

---

## Implementation Order

### Iteration 1: WikiLinkAutocompleteViewModel
- Most complex feature
- High value (used constantly)
- Good test for the decomposition pattern

### Iteration 2: ExternalLinkPreviewViewModel
- Medium complexity
- Clear boundaries
- Independent of other features

### Iteration 3: ImageUploadViewModel
- Medium complexity
- JSInterop heavy
- Good for testing patterns

### Iteration 4: EditorViewModel
- Core functionality
- Timer management
- JSInterop coordination

### Iteration 5: Component Integration
- Wire all ViewModels into component
- Replace direct service calls
- Update JSInvokable callbacks
- Remove obsolete code
- Final testing

---

## Expected Outcomes

### Before Refactoring
- ArticleDetail.razor: 1,289 lines
- Mixed concerns: CRUD, autocomplete, preview, upload, editor
- Direct service dependencies: 16+ services
- Hard to test (requires full Blazor context)

### After Refactoring
- ArticleDetail.razor: ~400 lines (mostly markup + ViewModel coordination)
- Core ViewModel: 466 lines (4 components)
- Autocomplete ViewModel: 340 lines
- Preview ViewModel: 230 lines
- Upload ViewModel: 265 lines
- Editor ViewModel: 310 lines
- **Total: ~2,011 lines** (vs 1,289) BUT:
  - Each piece is focused and testable
  - ~150+ comprehensive unit tests
  - Clear separation of concerns
  - Easy to understand and maintain

### Test Coverage Goals
- Core ViewModel: 27 tests ✅ COMPLETE
- Autocomplete: 20 tests
- Preview: 10 tests
- Upload: 12 tests
- Editor: 15 tests
- **Total: ~84 ViewModel tests**
- Plus facade: 33 tests ✅ COMPLETE
- **Grand Total: 117+ tests**

---

## Success Criteria

✅ Each ViewModel has single, clear responsibility  
✅ Each ViewModel is independently testable  
✅ All ViewModels have >90% code coverage  
✅ Component is < 500 lines (mostly markup)  
✅ Component has minimal logic (just coordination)  
✅ No direct service dependencies in component  
✅ All tests passing  
✅ Build succeeds with zero errors  
✅ Application functions identically to before refactoring  

---

## Next Steps

1. Review and approve this decomposition plan
2. Implement Phase 7D-2C (WikiLinkAutocompleteViewModel)
3. Test and verify
4. Continue with remaining ViewModels
5. Final component integration
6. Update documentation

---

## Questions for Review

1. **Is the decomposition granular enough?** Or should we split further?
2. **Are the ViewModel boundaries clear?** Any overlap concerns?
3. **Is the implementation order correct?** Should we prioritize differently?
4. **Do we want to add any other ViewModels?** (Breadcrumb, Icon management, etc.)
5. **Should we create facades for specialized features?** (e.g., WikiLinkFacade, ImageUploadFacade)
