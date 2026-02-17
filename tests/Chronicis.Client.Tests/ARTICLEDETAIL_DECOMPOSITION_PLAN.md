# ArticleDetail Component - Full Decomposition & Maintenance Plan

**Created:** February 16, 2026  
**Status:** CRITICAL - Component currently broken, needs systematic fix

---

## Current Crisis

### Problem
ArticleDetail.razor has **43 compilation errors** due to incomplete ViewModel integration.
- Started integration but didn't finish
- Old state fields removed but references remain
- Component is 1,160 lines with mixed concerns
- **Application will not build**

### Immediate Action Required
1. Fix all remaining `_article` → `ViewModel.Article` references
2. Fix all remaining `_editTitle` → `EditTitle` references  
3. Fix all remaining `_editBody` → `EditBody` references
4. Fix all remaining service call references
5. Get build passing
6. Test basic functionality

---

## Root Cause Analysis

**Why This Happened:**
1. Component is too large (1,160 lines) to refactor safely in one session
2. Too many concerns mixed together (CRUD + autocomplete + preview + upload + editor)
3. Attempted "big bang" integration instead of incremental migration
4. Didn't create specialized ViewModels first

**What We Should Have Done:**
1. Create ALL specialized ViewModels FIRST
2. Then integrate component incrementally
3. Test after each piece
4. Keep component building at all times

---

## Long-Term Solution: Complete Decomposition

### Current ViewModel Coverage

**What Our ViewModels Handle (Phase 7D-2A ✅):**
- Core CRUD: Load, Save, Delete, Create Child
- Edit state management
- Loading state management

**What They DON'T Handle:**
- Icon management (~30 lines)
- Autocomplete system (~200 lines)
- External link preview (~100 lines)
- Image upload (~150 lines)
- Editor lifecycle (~100 lines)
- Breadcrumb management (~50 lines)
- Keyboard shortcuts (~50 lines)
- WikiLink JSInterop callbacks (~150 lines)

### Proposed Additional ViewModels

#### 1. IconManagementViewModel
**Purpose:** Handle icon selection and updates  
**Lines:** ~60 lines (state + logic)  
**Tests:** ~8 tests  
**Removes from component:** ~30 lines

**Responsibilities:**
- Store current icon
- Update icon via facade
- Show notifications
- Update tree display

**Interface:**
```csharp
string? CurrentIcon { get; }
Task UpdateIconAsync(string? newIcon);
```

#### 2. WikiLinkAutocompleteViewModel
**Purpose:** Manage autocomplete popup  
**Lines:** ~220 lines  
**Tests:** ~18 tests  
**Removes from component:** ~200 lines

**Responsibilities:**
- Show/hide autocomplete
- Load suggestions (internal/external)
- Keyboard navigation
- Selection handling
- Article creation from autocomplete

**Interface:**
```csharp
bool IsVisible { get; }
bool IsLoading { get; }
List<WikiLinkAutocompleteItem> Suggestions { get; }
int SelectedIndex { get; }
Task TriggerAsync(string query, double x, double y);
void Hide();
Task SelectAsync(WikiLinkAutocompleteItem suggestion);
```

#### 3. ExternalLinkPreviewViewModel
**Purpose:** Manage preview drawer  
**Lines:** ~120 lines  
**Tests:** ~10 tests  
**Removes from component:** ~100 lines

**Responsibilities:**
- Open/close drawer
- Load external content
- Cache content
- Handle errors

**Interface:**
```csharp
bool IsOpen { get; }
bool IsLoading { get; }
ExternalLinkContentDto? Content { get; }
Task OpenAsync(string source, string id, string title);
void Close();
```

#### 4. ImageUploadViewModel
**Purpose:** Handle image operations  
**Lines:** ~150 lines  
**Tests:** ~12 tests  
**Removes from component:** ~150 lines

**Responsibilities:**
- Request upload URLs
- Track upload progress
- Confirm uploads
- Generate/resolve proxy URLs

**Interface:**
```csharp
bool IsUploading { get; }
Task<object?> RequestUploadAsync(string fileName, string contentType, long size);
Task ConfirmUploadAsync(string documentId);
string GetProxyUrl(string documentId);
Task<string?> ResolveUrlAsync(string documentId);
```

#### 5. EditorLifecycleViewModel
**Purpose:** Manage editor state  
**Lines:** ~150 lines  
**Tests:** ~15 tests  
**Removes from component:** ~100 lines

**Responsibilities:**
- Initialize TipTap editor
- Track editor state
- Handle content updates
- Auto-save coordination
- Editor destruction

**Interface:**
```csharp
bool IsInitialized { get; }
string Content { get; }
bool HasUnsavedChanges { get; }
Task InitializeAsync(string editorId, string initialContent);
void OnContentUpdate(string markdown);
Task DestroyAsync(string editorId);
```

#### 6. BreadcrumbViewModel (Optional)
**Purpose:** Manage breadcrumb display  
**Lines:** ~80 lines  
**Tests:** ~6 tests  
**Removes from component:** ~50 lines

**Responsibilities:**
- Build breadcrumb list
- Update page title
- Save last article path

**Interface:**
```csharp
List<BreadcrumbItem> Breadcrumbs { get; }
void UpdateBreadcrumbs(ArticleDto article);
Task UpdatePageTitleAsync(string title);
```

---

## Implementation Strategy

### Phase 1: Fix Current Build ⚠️ URGENT
**Goal:** Get application building and functional again

**Tasks:**
1. Create systematic find/replace script for remaining references
2. Test script on copy of file
3. Apply script to actual file
4. Build and fix any remaining errors
5. Test that CRUD operations work
6. Commit working state

**Estimated Time:** 2-3 hours  
**Risk:** Medium (lots of changes, but mechanical)

### Phase 2: Create Remaining ViewModels
**Goal:** Build out all specialized ViewModels

**Order:**
1. Icon Management (simplest, good test)
2. Breadcrumb Management (simple, independent)
3. External Link Preview (medium, clear boundaries)
4. Image Upload (medium-complex, JSInterop)
5. Editor Lifecycle (complex, core functionality)
6. WikiLink Autocomplete (most complex, highest value)

**Approach for Each:**
1. Create ViewModel interface
2. Create ViewModel implementation
3. Write comprehensive tests
4. Verify tests pass
5. DO NOT integrate into component yet

**Estimated Time:** 12-15 hours total (2-3 hours per ViewModel)  
**Risk:** Low (isolated, testable)

### Phase 3: Incremental Component Integration
**Goal:** Replace component logic with ViewModels one piece at a time

**Approach:**
1. Start with simplest (Icon, Breadcrumb)
2. Inject ViewModel into component
3. Replace 1 method/section at a time
4. Build and test after each change
5. Commit after each successful integration
6. Move to next ViewModel

**Key Principle:** NEVER break the build. Always keep component functional.

**Estimated Time:** 8-10 hours  
**Risk:** Medium-Low (incremental approach reduces risk)

### Phase 4: Cleanup & Documentation
**Goal:** Final polish and maintainability

**Tasks:**
1. Remove any remaining obsolete code
2. Update tests for component behavior
3. Update documentation
4. Performance testing
5. Code review

**Estimated Time:** 3-4 hours  
**Risk:** Low

---

## Success Metrics

### Component Size
- **Current:** 1,160 lines
- **Target:** <400 lines (mostly markup + ViewModel coordination)
- **Reduction:** ~66% smaller

### ViewModels Created
- **Core CRUD:** 4 components (✅ done)
- **Specialized:** 6 more components
- **Total:** 10 focused, testable components

### Test Coverage
- **Current:** 60 tests (facade + core ViewModels)
- **Target:** 117+ tests (all ViewModels covered)
- **Component:** Minimal tests (just coordination logic)

### Maintainability
- ✅ Each ViewModel < 250 lines
- ✅ Single Responsibility per ViewModel
- ✅ No direct service dependencies in component
- ✅ All logic testable without Blazor context
- ✅ Clear separation of concerns

---

## Lessons Learned

### What Went Wrong
1. **Tried to integrate before all ViewModels were ready**
   - Should have built ALL ViewModels first
   - Integration would have been smoother

2. **Underestimated component complexity**
   - 1,160 lines is too large for safe refactoring in one go
   - Needed more granular decomposition plan upfront

3. **Didn't maintain build integrity**
   - Broke build partway through
   - Should have used feature branches or checkpoints

### What To Do Next Time
1. **Complete decomposition BEFORE integration**
   - Build all ViewModels
   - Test all ViewModels
   - THEN integrate

2. **Integrate incrementally with checkpoints**
   - One ViewModel at a time
   - Build + test after each
   - Commit after each success

3. **Use feature flags or branches**
   - Keep main branch building
   - Use feature flag to toggle new vs old code paths

---

## Immediate Next Steps (Priority Order)

1. **URGENT: Fix Build** (Phase 1)
   - Script to replace all remaining references
   - Test and verify
   - Get application functional

2. **Create Remaining ViewModels** (Phase 2)
   - Start with IconManagementViewModel (simplest)
   - Build them all before integrating

3. **Integrate Incrementally** (Phase 3)
   - One ViewModel at a time
   - Keep build passing
   - Test thoroughly

4. **Polish & Document** (Phase 4)
   - Final cleanup
   - Update docs
   - Celebrate completion

---

## Risk Mitigation

### If We Get Stuck Again
1. **Revert to last working commit**
   - Don't stay broken for long
   - Always have working fallback

2. **Create safety branch**
   - Branch before risky changes
   - Easy to abandon if needed

3. **Smaller increments**
   - If integration fails, break into even smaller steps
   - One method at a time if needed

### If ViewModels Get Too Complex
1. **Further decompose**
   - Break large ViewModels into sub-components
   - Maintain Single Responsibility

2. **Refactor before continuing**
   - Don't integrate messy ViewModels
   - Clean them up first

---

## Timeline Estimate

**Total Effort:** 25-35 hours

**Breakdown:**
- Phase 1 (Fix Build): 2-3 hours
- Phase 2 (ViewModels): 12-15 hours
- Phase 3 (Integration): 8-10 hours
- Phase 4 (Cleanup): 3-4 hours

**Calendar Time:** 1-2 weeks (depending on session frequency)

---

## Definition of Done

✅ All ViewModels created and tested  
✅ Component uses only ViewModels (no direct service calls)  
✅ Component < 400 lines  
✅ 117+ tests passing  
✅ Zero compilation errors  
✅ All features functional  
✅ Documentation updated  
✅ Code review complete  

---

**This is a maintainable path forward. Let's execute it systematically.**
