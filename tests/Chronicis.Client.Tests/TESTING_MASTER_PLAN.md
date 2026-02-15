# Chronicis Client Testing Master Plan

**Project:** Chronicis - D&D Campaign Knowledge Management  
**Last Updated:** February 15, 2026  
**Status:** Phase 4 Complete, Phase 5 (Refactoring) In Progress

---

## Table of Contents
1. [Core Testing Philosophy](#core-testing-philosophy)
2. [Project Context](#project-context)
3. [Testing Infrastructure](#testing-infrastructure)
4. [Completed Phases](#completed-phases)
5. [Current Phase](#current-phase)
6. [Remaining Work](#remaining-work)
7. [Testing Patterns Reference](#testing-patterns-reference)
8. [Component Inventory](#component-inventory)

---

## Core Testing Philosophy

### Dave's Key Principles

> **"Unit tests should be easy to write. If they're not, that's a major code smell."** - Dave

This is the **fundamental principle** guiding all testing work. When a component is hard to test, the problem is NOT the testing approach - it's the component design.

### Testing Quality Standards

1. **Easy to Write** - If you're struggling with complex mocking, the component needs refactoring
2. **Fast** - Component tests should execute in milliseconds, not seconds
3. **Focused** - One logical assertion per test
4. **Deterministic** - No flaky tests, no timing issues
5. **Maintainable** - Clear naming, minimal duplication

### Refactoring Over Mocking

When encountering components with multiple service dependencies:
- **DON'T:** Create complex service mocking infrastructure
- **DO:** Refactor the component to accept data as parameters
- **WHY:** Better design is the goal, not just test coverage

The test difficulty reveals design problems. Fix the design, and tests become trivial.

---

## Project Context

### Technology Stack
- **Framework:** Blazor WebAssembly (.NET 9)
- **UI Library:** MudBlazor
- **Testing:** xUnit + bUnit + NSubstitute
- **Build:** dotnet CLI

### File Locations
- **Source Components:** `Z:\repos\chronicis\src\Chronicis.Client\Components\`
- **Test Files:** `Z:\repos\chronicis\tests\Chronicis.Client.Tests\`
- **Component Tests:** `Z:\repos\chronicis\tests\Chronicis.Client.Tests\Components\`

### Build Commands
```powershell
# Build tests
cd Z:\repos\chronicis
dotnet build tests\Chronicis.Client.Tests\Chronicis.Client.Tests.csproj

# Run all tests
dotnet test Chronicis.sln

# Run specific test class
dotnet test tests\Chronicis.Client.Tests\Chronicis.Client.Tests.csproj --filter "FullyQualifiedName~ComponentNameTests"
```

---

## Testing Infrastructure

### Base Classes

**TestContext** - For components without MudBlazor dependencies
```csharp
public class MyComponentTests : TestContext
{
    [Fact]
    public void Component_Behavior()
    {
        var cut = RenderComponent<MyComponent>();
        // assertions
    }
}
```

**MudBlazorTestContext** - For components using MudBlazor
```csharp
public class MyComponentTests : MudBlazorTestContext
{
    [Fact]
    public void Component_WithMudBlazor()
    {
        var cut = RenderComponent<MyComponent>();
        var button = cut.FindComponent<MudButton>();
        // assertions
    }
}
```

### Key Files
- `MudBlazorTestContext.cs` - Base class that configures MudBlazor services and JSInterop
- Located in: `Z:\repos\chronicis\tests\Chronicis.Client.Tests\Components\`

### NuGet Packages
- bUnit (1.32.7)
- NSubstitute (5.3.0) - For service mocking when necessary
- xUnit (2.6.6)

---

## Completed Phases

### Phase 1: Architectural Tests ✅
**Status:** Complete  
**Tests:** 30 tests  
**Focus:** Reflection-based convention enforcement

**Achievements:**
- Service interface conventions
- DTO naming conventions
- API endpoint patterns

**Location:** `Z:\repos\chronicis\tests\Chronicis.ArchitecturalTests\`

### Phase 2: Service Layer Tests ✅
**Status:** Complete  
**Tests:** 106 tests across 7 services  
**Focus:** Business logic services without dependencies

**Services Tested:**
1. AppContextService (18 tests)
2. BreadcrumbService (24 tests)
3. MetadataDrawerService (3 tests)
4. QuestDrawerService (16 tests)
5. KeyboardShortcutService (4 tests)
6. MarkdownService (38 tests)
7. WikiLinkService (13 tests)

**Location:** `Z:\repos\chronicis\tests\Chronicis.Client.Tests\Services\`

### Phase 3: Tree State Tests ✅
**Status:** Complete  
**Tests:** 65 tests  
**Focus:** TreeStateService functionality

**Location:** `Z:\repos\chronicis\tests\Chronicis.Client.Tests\Tree*Tests.cs`

### Phase 4: Simple Component Tests ✅
**Status:** Complete  
**Tests:** 181 tests across 17 components  
**Focus:** Components without service dependencies OR already well-designed

**Components Tested:**

**Presentational (6 components, 52 tests):**
- EmptyState (9) - Empty states with actions
- LoadingSkeleton (5) - Loading skeletons
- PublicFooter (14) - Footer navigation
- NotFoundAlert (7) - Not found warnings
- IconDisplay (14) - Icon rendering
- AuthorizingScreen (8) - Auth loading screen

**Interactive (4 components, 61 tests):**
- EntityListItem (11) - Clickable list items
- DetailPageHeader (16) - Page headers with breadcrumbs
- SearchResultCard (17) - Search results with highlighting
- ArticleActionBar (17) - Article actions

**Status & Navigation (7 components, 68 tests):**
- QuestStatusChip (10) - Status chips
- SaveStatusIndicator (12) - Save status display
- ChroniclsBreadcrumbs (13) - Breadcrumb wrapper
- PromptPanel (15) - Contextual prompts
- RedirectToDashboard (3) - Dashboard redirect
- RedirectToLogin (3) - Login redirect
- BacklinksPanel (7) - Backlinks display (REFACTORED)

**Location:** `Z:\repos\chronicis\tests\Chronicis.Client.Tests\Components\`

---

## Current Phase

### Phase 5: TDD Refactoring for Testability 🔄
**Status:** IN PROGRESS  
**Approach:** Test-Driven Development for design improvement

### What This Phase Is About

Components with multiple service dependencies are **poorly designed**, not "hard to test." The hard testing reveals the design flaw. Phase 5 fixes the design through TDD refactoring.

### TDD Refactoring Process

**For each complex component:**

1. **Analyze Current Design**
   - List all service dependencies
   - Identify mixed concerns
   - Document complex initialization logic

2. **Write Characterization Tests** (if valuable)
   - Capture current behavior
   - Validate with passing tests
   - These are temporary - will be deleted

3. **Design Better API**
   - Extract data parameters
   - Create event callbacks
   - Remove service dependencies from component

4. **Refactor Component**
   - Make component pure presentation
   - Accept data as parameters
   - Delegate logic to parent

5. **Update Consumers**
   - Parent component manages data
   - Parent handles service calls
   - Parent passes data down

6. **Write Simple Tests**
   - No mocking needed
   - Just pass test data
   - Instant assertions

7. **Verify Everything Passes**
   - Build succeeds
   - All tests pass
   - Delete characterization tests

### Example: BacklinksPanel Refactoring ✅

**Before (Complex):**
```csharp
@inject ILinkApiService LinkApi
@inject IArticleCacheService ArticleCache
@inject NavigationManager Navigation
@inject ILogger Logger

[Parameter] public Guid ArticleId { get; set; }
[Parameter] public bool IsOpen { get; set; }

protected override async Task OnParametersSetAsync()
{
    // Complex loading logic
    // State tracking
    // Async data fetching
}
```

**Test Before (Complex):**
```csharp
var mockLinkApi = Substitute.For<ILinkApiService>();
var mockCache = Substitute.For<IArticleCacheService>();
var mockNav = Substitute.For<NavigationManager>();
var mockLogger = Substitute.For<ILogger>();
Services.AddSingleton(mockLinkApi);
Services.AddSingleton(mockCache);
Services.AddSingleton(mockNav);
Services.AddSingleton(mockLogger);

mockLinkApi.GetBacklinksAsync(id).Returns(data);

var cut = RenderComponent<BacklinksPanel>(p => p
    .Add(x => x.ArticleId, id)
    .Add(x => x.IsOpen, true));
    
await Task.Delay(200); // Wait for async load
Assert.Contains("expected", cut.Markup);
```

**After (Simple):**
```csharp
// NO service dependencies!

[Parameter, EditorRequired]
public List<BacklinkDto> Backlinks { get; set; } = new();

[Parameter]
public bool IsLoading { get; set; }

[Parameter]
public EventCallback<Guid> OnNavigateToArticle { get; set; }

// Pure presentation - just renders data
```

**Test After (Simple):**
```csharp
var backlinks = new List<BacklinkDto>
{
    new() { ArticleId = Guid.NewGuid(), Title = "Test" }
};

var cut = RenderComponent<BacklinksPanel>(p => p
    .Add(x => x.Backlinks, backlinks));

Assert.Contains("Test", cut.Markup); // Instant!
```

**Parent Component Manages Data:**
```csharp
// ArticleMetadataDrawer.razor now handles data loading
@inject ILinkApiService LinkApi
@inject IArticleCacheService ArticleCache
@inject NavigationManager Navigation

private List<BacklinkDto> _backlinks = new();
private bool _loadingBacklinks = false;

protected override async Task OnParametersSetAsync()
{
    if (IsOpen && Article.Id != _lastArticleId)
    {
        await LoadBacklinksAsync();
    }
}

private async Task LoadBacklinksAsync()
{
    _loadingBacklinks = true;
    _backlinks = await LinkApi.GetBacklinksAsync(Article.Id);
    _loadingBacklinks = false;
}

private async Task HandleNavigateToArticle(Guid id)
{
    var path = await ArticleCache.GetNavigationPathAsync(id);
    Navigation.NavigateTo($"/article/{path}");
}

<BacklinksPanel Backlinks="_backlinks"
                IsLoading="_loadingBacklinks"
                OnNavigateToArticle="HandleNavigateToArticle" />
```

### Benefits Achieved

**Code Quality:**
- ✅ Single Responsibility Principle (component just renders)
- ✅ Dependency Inversion (parent controls behavior)
- ✅ Easier to understand (less code, clearer purpose)

**Testing:**
- ✅ No mocking required
- ✅ Fast execution (no async waits)
- ✅ Deterministic (no timing issues)
- ✅ Easy to write (just pass data)

### Refactoring Patterns

**Pattern 1: Data Parameters (Simple Components)**
- Component accepts List<DTO> as parameter
- Component accepts bool IsLoading for state
- Component accepts EventCallback for interactions
- Parent handles all service calls

**Pattern 2: ViewModel (Components with Logic)**
```csharp
public class SearchBoxViewModel
{
    public string SearchText { get; set; }
    public bool HasText => !string.IsNullOrWhiteSpace(SearchText);
    public event Action? OnSearch;
    
    public void ExecuteSearch() => OnSearch?.Invoke();
}

// Component
[Parameter] public SearchBoxViewModel ViewModel { get; set; }
```

**Pattern 3: Facade Service (Very Complex Components)**
```csharp
public interface IWorldCampaignFacade
{
    Task<WorldCampaignState> GetStateAsync();
    Task<WorldCampaignState> SelectWorldAsync(Guid worldId);
}

// Component uses single facade instead of 5 services
@inject IWorldCampaignFacade Facade
```

---

## Remaining Work

### Components Requiring Refactoring

**High Priority (Similar to BacklinksPanel):**

1. **OutgoingLinksPanel** - Same pattern as BacklinksPanel
   - Current: ArticleId parameter, fetches data internally
   - Target: Accept List<OutgoingLinkDto>, IsLoading, OnNavigate
   - Consumer: ArticleMetadataDrawer manages data

2. **ExternalLinksPanel** - Same pattern
   - Current: ArticleId parameter, fetches data internally
   - Target: Accept List<ExternalLinkDto>, IsLoading, OnNavigate
   - Consumer: ArticleMetadataDrawer manages data

**Medium Priority (Need ViewModel):**

3. **SearchBox** - TreeStateService dependency
   - Current: Direct TreeStateService injection
   - Target: SearchBoxViewModel with search state
   - Consumer: Parent (ArticleTreeView or Layout) manages TreeStateService

4. **CharacterClaimButton** - ICharacterApiService dependency
   - Current: Fetches claim status, handles claim/unclaim
   - Target: Accept ClaimStatus DTO, EventCallback for actions
   - Consumer: Parent manages API calls

**Lower Priority (Complex - Need Facades):**

5. **WorldCampaignSelector** - Multiple services
   - Current: IAppContextService, IDialogService, ISnackbar
   - Target: IWorldCampaignFacade or ViewModel pattern
   - Requires: New facade interface

6. **AISummarySection** - Extremely complex (500+ lines)
   - Current: IAISummaryApiService + complex state management
   - Target: May need multiple smaller components + facade
   - Requires: Significant architectural planning

**Deferred (Until Architecture Decisions Made):**

7. **ArticleDetail** - Needs page-level refactoring discussion
8. **Dialog Components** - Need dialog pattern established
9. **ArticleTreeView** - Core navigation, needs careful approach
10. **PublicNav** - AuthorizeView complexity

### Services Still Without Tests

From Phase 3 backlog (lower priority):

1. WikiLinkAutocompleteService
2. ArticleCacheService
3. QuoteService
4. RenderDefinitionService
5. RenderDefinitionGeneratorService

These have dependencies but are **service layer**, not UI. Can mock dependencies at service layer testing.

---

## Testing Patterns Reference

### Pattern 1: Simple Component (No Dependencies)

```csharp
public class ComponentTests : TestContext
{
    [Fact]
    public void Component_RendersContent()
    {
        var cut = RenderComponent<MyComponent>(parameters => parameters
            .Add(p => p.Title, "Test Title"));
        
        Assert.Contains("Test Title", cut.Markup);
    }
}
```

### Pattern 2: MudBlazor Component

```csharp
public class ComponentTests : MudBlazorTestContext
{
    [Fact]
    public void Component_RendersMudBlazor()
    {
        var cut = RenderComponent<MyComponent>();
        
        var button = cut.FindComponent<MudButton>();
        Assert.NotNull(button);
    }
}
```

### Pattern 3: Event Callback Testing

```csharp
[Fact]
public async Task Component_TriggersCallback()
{
    var callbackInvoked = false;
    
    var cut = RenderComponent<MyComponent>(parameters => parameters
        .Add(p => p.OnClick, () => callbackInvoked = true));
    
    var button = cut.Find("button");
    await button.ClickAsync(new MouseEventArgs());
    
    Assert.True(callbackInvoked);
}
```

### Pattern 4: Theory Tests (Enum Variations)

```csharp
[Theory]
[InlineData(Status.Active, Color.Success)]
[InlineData(Status.Pending, Color.Warning)]
[InlineData(Status.Failed, Color.Error)]
public void Component_MapsStatusToColor(Status status, Color expectedColor)
{
    var cut = RenderComponent<MyComponent>(parameters => parameters
        .Add(p => p.Status, status));
    
    var chip = cut.FindComponent<MudChip<string>>();
    Assert.Equal(expectedColor, chip.Instance.Color);
}
```

### Pattern 5: Navigation Testing

```csharp
[Fact]
public void Component_NavigatesCorrectly()
{
    var navMan = Services.GetRequiredService<FakeNavigationManager>();
    
    var cut = RenderComponent<RedirectComponent>();
    
    Assert.EndsWith("/expected-path", navMan.Uri);
}
```

### Pattern 6: Finding Elements

```csharp
// By CSS class
var element = cut.Find(".my-class");

// By content
var button = cut.FindAll("button")
    .First(b => b.TextContent.Contains("Save"));

// By component type
var mudButton = cut.FindComponent<MudButton>();
```

### Pattern 7: Service Mocking (When Necessary)

```csharp
// Use NSubstitute for service mocking
var mockService = Substitute.For<IMyService>();
mockService.GetDataAsync(Arg.Any<Guid>()).Returns(testData);

Services.AddSingleton(mockService);

var cut = RenderComponent<MyComponent>();

// Verify call
await mockService.Received(1).GetDataAsync(Arg.Any<Guid>());
```

### Anti-Patterns to Avoid

❌ **Multiple service mocks** → Refactor component instead  
❌ **Task.Delay() in tests** → Make component accept data  
❌ **Complex setup** → Component is too complex  
❌ **Brittle markup matching** → Use semantic selectors  
❌ **Testing implementation details** → Test behavior  

---

## Component Inventory

### Full Component List by Status

**✅ Tested (17 components)**
- EmptyState
- LoadingSkeleton
- PublicFooter
- NotFoundAlert
- IconDisplay
- AuthorizingScreen
- EntityListItem
- DetailPageHeader
- SearchResultCard
- ArticleActionBar
- QuestStatusChip
- SaveStatusIndicator
- ChroniclsBreadcrumbs
- PromptPanel
- RedirectToDashboard
- RedirectToLogin
- BacklinksPanel (refactored)

**🔄 Needs Refactoring Then Testing (6 components)**
- OutgoingLinksPanel (high priority - same as BacklinksPanel)
- ExternalLinksPanel (high priority - same as BacklinksPanel)
- SearchBox (medium priority - needs ViewModel)
- CharacterClaimButton (medium priority - data parameters)
- WorldCampaignSelector (low priority - needs facade)
- AISummarySection (low priority - very complex)

**⏸️ Deferred (Architectural Decisions Needed)**
- ArticleDetail
- ArticleTreeView
- ArticleTreeNode
- Dialog components (all)
- PublicNav
- ArticleHeader
- ArticleMetadataDrawer (partially refactored)
- QuestDrawer
- MarkdownToolbar
- QuickAddSession

### By Location

**Articles/** (6 tested, 4 need refactoring, 5 deferred)
- ✅ BacklinksPanel
- 🔄 OutgoingLinksPanel
- 🔄 ExternalLinksPanel
- ⏸️ ArticleDetail
- ⏸️ ArticleTreeView
- ⏸️ ArticleTreeNode
- ⏸️ ArticleHeader
- ⏸️ ArticleMetadataDrawer
- ⏸️ MarkdownToolbar
- ⏸️ QuickAddSession

**Shared/** (9 tested, 0 need refactoring)
- ✅ EmptyState
- ✅ LoadingSkeleton
- ✅ NotFoundAlert
- ✅ IconDisplay
- ✅ EntityListItem
- ✅ DetailPageHeader
- ✅ SearchResultCard
- ✅ ArticleActionBar
- ✅ PromptPanel

**Quests/** (1 tested, 0 need refactoring, 1 deferred)
- ✅ QuestStatusChip
- ⏸️ QuestDrawer

**Routing/** (3 tested)
- ✅ RedirectToDashboard
- ✅ RedirectToLogin
- ✅ AuthorizingScreen

**Characters/** (0 tested, 1 needs refactoring)
- 🔄 CharacterClaimButton

**World/** (0 tested, 1 needs refactoring)
- 🔄 WorldCampaignSelector

**Layout/** (1 tested, 1 needs refactoring, 1 deferred)
- ✅ PublicFooter
- 🔄 SearchBox
- ⏸️ PublicNav

**Dashboard/** (1 tested)
- ✅ SaveStatusIndicator

**Context/** (1 tested)
- ✅ ChroniclsBreadcrumbs

---

## Current Statistics

### Test Counts

| Project | Tests | Status |
|---------|-------|--------|
| ArchitecturalTests | 30 | ✅ Complete |
| Api.Tests | 266 | ✅ Complete (2 skipped) |
| **Client.Tests** | **352** | **🔄 In Progress** |
| Shared.Tests | 353 | ✅ Complete |
| ResourceCompiler.Tests | 28 | ✅ Complete |
| **TOTAL** | **1,040** | **99.8% passing** |

### Client Test Breakdown

| Category | Tests | Components/Services |
|----------|-------|---------------------|
| Component Tests | 181 | 17 components |
| Service Tests | 106 | 7 services |
| Tree Tests | 65 | TreeStateService |
| **TOTAL** | **352** | **Complete Phase 4** |

---

## Success Criteria

### For Each Component Test Suite

- ✅ **Build succeeds** with 0 errors
- ✅ **All tests pass** (100% pass rate)
- ✅ **Fast execution** (< 100ms per component suite)
- ✅ **No service mocking** (or minimal, if unavoidable)
- ✅ **Clear test names** describing exact scenario
- ✅ **Focused assertions** (one logical check per test)

### For Refactored Components

- ✅ **Fewer dependencies** than before
- ✅ **Single responsibility** (just presentation)
- ✅ **Easy to test** (< 5 lines of setup)
- ✅ **Parent manages data** (clear separation)
- ✅ **All consumers updated** and working

---

## Next Steps for Continuation

### Immediate Next Component: OutgoingLinksPanel

1. **Read current implementation**
   ```
   Z:\repos\chronicis\src\Chronicis.Client\Components\Articles\OutgoingLinksPanel.razor
   ```

2. **Follow BacklinksPanel pattern**
   - Copy BacklinksPanelTests.cs as template
   - Refactor OutgoingLinksPanel to accept data parameters
   - Update ArticleMetadataDrawer to load outgoing links
   - Write simple tests

3. **Verify**
   ```powershell
   dotnet build tests\Chronicis.Client.Tests\Chronicis.Client.Tests.csproj
   dotnet test tests\Chronicis.Client.Tests\Chronicis.Client.Tests.csproj --filter "FullyQualifiedName~OutgoingLinksPanel"
   ```

### After OutgoingLinksPanel

Continue with **ExternalLinksPanel** using the exact same pattern.

Then move to **SearchBox** which needs the ViewModel pattern.

---

## Key Documentation Files

### In Test Project
- `TESTING_MASTER_PLAN.md` (this file) - Overall strategy and status
- `README.md` - Navigation hub for Phase 4 docs
- `TESTABILITY_REFACTORING_PLAN.md` - Detailed refactoring patterns
- `TDD_REFACTORING_PROGRESS.md` - BacklinksPanel refactoring example

### Component Documentation
- Each component test file includes header comments explaining approach
- MudBlazorTestContext.cs has setup documentation
- Phase 4 completion docs in Components folder

---

## Important Reminders

### Dave's Testing Philosophy
1. **"If tests are hard to write, it's a code smell"**
2. **Refactor the component, don't build complex test infrastructure**
3. **Tests should be trivial if the design is good**

### TDD Approach
1. Understand current behavior
2. Refactor component design
3. Update consumers
4. Write simple tests
5. Verify all tests pass

### Quality Over Coverage
- Better to have 10 well-designed, tested components
- Than 50 components with complex, brittle tests
- The goal is BETTER CODE, not just test counts

---

**Document Status:** Living Document  
**Maintained By:** AI-Assisted Development Process  
**Review Frequency:** After each component refactoring  
**Last Major Update:** BacklinksPanel TDD refactoring complete
