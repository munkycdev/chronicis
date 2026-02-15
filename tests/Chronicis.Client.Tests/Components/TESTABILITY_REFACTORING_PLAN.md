# Component Testability Refactoring Plan

## Problem Statement

Many Chronicis components are difficult to test because they:
1. **Inject multiple services directly** - tight coupling
2. **Mix presentation and business logic** - violates SRP
3. **Perform data fetching in OnInitialized** - hard to control in tests
4. **Have complex initialization flows** - difficult to set up test state

This is a **major code smell** indicating poor separation of concerns.

## Architecture Principle

> **"If a component is hard to test, it's poorly designed."**

Unit tests should be trivial to write. Difficulty testing reveals:
- Tight coupling to implementation details
- Missing abstractions
- Mixed responsibilities
- Poor separation of concerns

## Refactoring Strategy

### Pattern 1: Extract View Models (Recommended)

**Before (SearchBox.razor):**
```csharp
@inject ITreeStateService TreeState

<MudTextField @bind-Value="_searchText" />

@code {
    private string _searchText = string.Empty;
    
    protected override void OnInitialized()
    {
        TreeState.OnStateChanged += StateHasChanged;
    }
    
    private Task ExecuteSearch()
    {
        TreeState.SetSearchQuery(_searchText);
        return Task.CompletedTask;
    }
}
```

**After (SearchBox.razor with ViewModel):**
```csharp
@code {
    [Parameter, EditorRequired]
    public SearchBoxViewModel ViewModel { get; set; } = null!;
}

<MudTextField @bind-Value="ViewModel.SearchText" 
              @onkeydown="ViewModel.HandleKeyDown" />
```

**SearchBoxViewModel.cs:**
```csharp
public class SearchBoxViewModel
{
    public string SearchText { get; set; } = string.Empty;
    public event Action? OnSearch;
    
    public void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            OnSearch?.Invoke();
    }
}
```

**Benefits:**
- Component is pure presentation - trivial to test
- ViewModel is POCO - easy to test with no Blazor
- Parent page handles service coordination
- Clear separation of concerns

### Pattern 2: Pass Data as Parameters (Simple Components)

**Before (BacklinksPanel.razor):**
```csharp
@inject ILinkApiService LinkApi
@inject IArticleCacheService ArticleCache

@code {
    [Parameter] public Guid ArticleId { get; set; }
    
    private List<BacklinkDto> _backlinks = new();
    
    protected override async Task OnParametersSetAsync()
    {
        _backlinks = await LinkApi.GetBacklinksAsync(ArticleId);
    }
}
```

**After (BacklinksPanel.razor):**
```csharp
@code {
    [Parameter, EditorRequired]
    public List<BacklinkDto> Backlinks { get; set; } = new();
    
    [Parameter]
    public EventCallback<Guid> OnNavigateToArticle { get; set; }
}
```

**Benefits:**
- Component is just a renderer - no logic to test
- Parent handles data fetching
- Can test with any data easily
- No service mocking needed

### Pattern 3: Facade Service (Complex Components)

**Before (WorldCampaignSelector.razor):**
```csharp
@inject IAppContextService AppContext
@inject IDialogService DialogService
@inject ISnackbar Snackbar

@code {
    // Complex initialization
    // Multiple service calls
    // State management
}
```

**After (WorldCampaignSelector.razor):**
```csharp
@inject IWorldCampaignFacade Facade

@code {
    [Parameter]
    public WorldCampaignState State { get; set; } = new();
    
    private async Task OnWorldChanged(Guid id)
    {
        State = await Facade.SelectWorldAsync(id);
    }
}
```

**IWorldCampaignFacade.cs:**
```csharp
public interface IWorldCampaignFacade
{
    Task<WorldCampaignState> GetStateAsync();
    Task<WorldCampaignState> SelectWorldAsync(Guid worldId);
    Task<WorldCampaignState> SelectCampaignAsync(Guid? campaignId);
}
```

**Benefits:**
- Single service to mock
- Clear contract
- State-based testing
- Business logic in facade (unit testable)

---

## Specific Component Refactorings

### 1. SearchBox → SearchBoxViewModel

**Current Issues:**
- Depends on ITreeStateService
- Event subscription in OnInitialized
- Mixed UI and logic

**Refactoring:**
```csharp
// SearchBoxViewModel.cs
public class SearchBoxViewModel : IDisposable
{
    private readonly ITreeStateService _treeState;
    
    public string SearchText { get; set; } = string.Empty;
    public bool HasText => !string.IsNullOrWhiteSpace(SearchText);
    
    public SearchBoxViewModel(ITreeStateService treeState)
    {
        _treeState = treeState;
        _treeState.OnStateChanged += NotifyStateChanged;
    }
    
    public event Action? OnStateChanged;
    
    public void ExecuteSearch()
    {
        _treeState.SetSearchQuery(SearchText);
    }
    
    public void ClearSearch()
    {
        SearchText = string.Empty;
        _treeState.ClearSearch();
    }
    
    public void Dispose()
    {
        _treeState.OnStateChanged -= NotifyStateChanged;
    }
    
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}

// SearchBox.razor
@code {
    [Parameter, EditorRequired]
    public SearchBoxViewModel ViewModel { get; set; } = null!;
    
    protected override void OnInitialized()
    {
        ViewModel.OnStateChanged += StateHasChanged;
    }
    
    public void Dispose()
    {
        ViewModel.OnStateChanged -= StateHasChanged;
    }
}
```

**Test:**
```csharp
[Fact]
public void SearchBox_RendersViewModel()
{
    var vm = new SearchBoxViewModel(Mock.Of<ITreeStateService>());
    vm.SearchText = "test search";
    
    var cut = RenderComponent<SearchBox>(p => p
        .Add(x => x.ViewModel, vm));
    
    Assert.Contains("test search", cut.Find("input").GetAttribute("value"));
}
```

### 2. BacklinksPanel → Data Parameters

**Current Issues:**
- Fetches data directly
- Complex OnParametersSetAsync logic
- Hard to test different states

**Refactoring:**
```csharp
// BacklinksPanel.razor
@code {
    [Parameter, EditorRequired]
    public List<BacklinkDto> Backlinks { get; set; } = new();
    
    [Parameter]
    public bool IsLoading { get; set; }
    
    [Parameter]
    public EventCallback<Guid> OnNavigateToArticle { get; set; }
}

// Parent (ArticleDetail.razor) handles data fetching:
@code {
    private List<BacklinkDto> _backlinks = new();
    private bool _loadingBacklinks = false;
    
    private async Task LoadBacklinks()
    {
        _loadingBacklinks = true;
        _backlinks = await LinkApi.GetBacklinksAsync(ArticleId);
        _loadingBacklinks = false;
    }
}

<BacklinksPanel Backlinks="_backlinks" 
                IsLoading="_loadingBacklinks"
                OnNavigateToArticle="HandleNavigate" />
```

**Test:**
```csharp
[Fact]
public void BacklinksPanel_DisplaysBacklinks()
{
    var backlinks = new List<BacklinkDto>
    {
        new() { ArticleId = Guid.NewGuid(), Title = "Article 1" },
        new() { ArticleId = Guid.NewGuid(), Title = "Article 2" }
    };
    
    var cut = RenderComponent<BacklinksPanel>(p => p
        .Add(x => x.Backlinks, backlinks));
    
    Assert.Contains("Article 1", cut.Markup);
    Assert.Contains("Article 2", cut.Markup);
}

[Fact]
public void BacklinksPanel_ShowsEmptyState_WhenNoBacklinks()
{
    var cut = RenderComponent<BacklinksPanel>(p => p
        .Add(x => x.Backlinks, new List<BacklinkDto>()));
    
    Assert.Contains("No incoming links", cut.Markup);
}
```

### 3. AISummarySection → Facade + ViewModel

**Current Issues:**
- 500+ lines
- Multiple services
- Complex state management
- Mixed concerns

**Refactoring:**
```csharp
// AISummarySectionViewModel.cs
public class AISummarySectionViewModel
{
    public bool IsExpanded { get; set; }
    public bool IsLoading { get; set; }
    public string? Summary { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public List<SummaryTemplateDto> Templates { get; set; } = new();
    public Guid? SelectedTemplateId { get; set; }
    public string? CustomPrompt { get; set; }
    public SummaryEstimateDto? Estimate { get; set; }
    
    public bool HasSummary => !string.IsNullOrEmpty(Summary);
    public string RelativeTime => GeneratedAt.HasValue 
        ? FormatRelativeTime(GeneratedAt.Value) 
        : "";
}

// IAISummaryFacade.cs
public interface IAISummaryFacade
{
    Task<AISummarySectionViewModel> LoadAsync(Guid entityId, string entityType);
    Task<AISummarySectionViewModel> GenerateAsync(Guid entityId, GenerateSummaryRequestDto request);
    Task<AISummarySectionViewModel> ClearAsync(Guid entityId);
}

// AISummarySection.razor (now much simpler)
@code {
    [Parameter, EditorRequired]
    public AISummarySectionViewModel ViewModel { get; set; } = null!;
    
    [Parameter]
    public EventCallback<GenerateSummaryRequestDto> OnGenerate { get; set; }
}
```

**Test:**
```csharp
[Fact]
public void AISummarySection_DisplaysSummary()
{
    var vm = new AISummarySectionViewModel
    {
        Summary = "Test summary",
        GeneratedAt = DateTime.UtcNow,
        HasSummary = true
    };
    
    var cut = RenderComponent<AISummarySection>(p => p
        .Add(x => x.ViewModel, vm));
    
    Assert.Contains("Test summary", cut.Markup);
}
```

---

## Implementation Phases

### Phase 1: Identify Patterns (1 hour)
- [x] Analyze components for testability issues ✅ (done above)
- [x] Categorize by refactoring pattern needed
- [x] Document current pain points

### Phase 2: Simple Components First (2-3 hours)
Extract data parameters for simple display components:
1. BacklinksPanel → data parameters
2. OutgoingLinksPanel → data parameters  
3. ExternalLinksPanel → data parameters

**Effort:** Low (simple change)
**Impact:** High (3 components testable immediately)

### Phase 3: ViewModels for Medium Components (3-4 hours)
Create ViewModels for components with moderate logic:
1. SearchBox → SearchBoxViewModel
2. CharacterClaimButton → CharacterClaimViewModel
3. QuickAddSession → QuickAddViewModel

**Effort:** Medium (new pattern to establish)
**Impact:** High (establishes pattern for future)

### Phase 4: Facades for Complex Components (4-6 hours)
Create facade services for multi-service components:
1. WorldCampaignSelector → IWorldCampaignFacade
2. AISummarySection → IAISummaryFacade
3. ArticleDetail → IArticleDetailFacade

**Effort:** High (requires service layer work)
**Impact:** Very High (solves hardest cases)

### Phase 5: Test Coverage (2-3 hours per phase)
Write tests after each refactoring phase

---

## Benefits of Refactoring

### Before
- ❌ Components hard to test (need complex mocks)
- ❌ Mixed responsibilities (UI + data + logic)
- ❌ Tight coupling to services
- ❌ Difficult to reason about
- ❌ Hard to reuse logic

### After  
- ✅ Components trivial to test (just props)
- ✅ Clear separation of concerns
- ✅ Loose coupling via interfaces
- ✅ Easy to understand
- ✅ Reusable ViewModels and facades

### Testing Improvements
- ✅ No service mocking needed for components
- ✅ Can test with simple DTOs
- ✅ ViewModels testable without Blazor
- ✅ Fast, focused unit tests
- ✅ Clear test intent

---

## Architecture Improvements

### Current Architecture Issues
```
Component
  ├─ UI Rendering (Razor)
  ├─ Business Logic (C# code block)
  ├─ Data Fetching (service calls)
  ├─ State Management (fields)
  └─ Event Handling (methods)
```
**Problem:** Everything mixed together

### Improved Architecture
```
Component (Pure Presentation)
  └─ Renders ViewModel/Props

ViewModel (Business Logic)
  ├─ Properties
  ├─ Commands
  └─ Events

Facade (Service Coordination)
  ├─ Orchestrates multiple services
  ├─ Maps to ViewModels
  └─ Handles complexity

Services (Data/Infrastructure)
  └─ Single responsibility
```
**Benefit:** Clear separation, easy testing

---

## Testing Strategy After Refactoring

### Component Tests (Trivial)
```csharp
[Fact]
public void Component_RendersData()
{
    var vm = new ViewModel { Data = "test" };
    var cut = RenderComponent<Component>(p => p.Add(x => x.ViewModel, vm));
    Assert.Contains("test", cut.Markup);
}
```

### ViewModel Tests (No Blazor)
```csharp
[Fact]
public void ViewModel_CalculatesCorrectly()
{
    var vm = new ViewModel();
    vm.SetValue(5);
    Assert.Equal(10, vm.DoubleValue);
}
```

### Facade Tests (Focused Integration)
```csharp
[Fact]
public async Task Facade_CoordinatesServices()
{
    var serviceA = Mock.Of<IServiceA>();
    var serviceB = Mock.Of<IServiceB>();
    var facade = new Facade(serviceA, serviceB);
    
    var result = await facade.DoSomethingAsync();
    
    Assert.NotNull(result);
    Mock.Get(serviceA).Verify(x => x.GetData(), Times.Once);
}
```

---

## Decision Points

### Should we refactor now or continue with current approach?

**Option A: Continue with Service Mocking (Phase 5 original plan)**
- Pros: Tests existing code as-is
- Cons: Accepts poor design, hard to maintain tests

**Option B: Refactor for Testability (Recommended)**
- Pros: Improves design, easier tests, better architecture
- Cons: Requires code changes, takes time

### Recommendation: **Option B - Refactor**

**Why:**
1. Code smells indicate design problems
2. Hard tests = poor design
3. Refactoring improves overall quality
4. Establishes good patterns for future
5. Makes testing trivial going forward

**Effort:** ~12-20 hours total
**Benefit:** Permanent improvement to codebase

---

## Next Steps (If Approved)

1. **Get approval** for refactoring approach
2. **Start with Phase 2** (simple data parameters)
3. **Validate pattern** with BacklinksPanel
4. **Iterate** through remaining components
5. **Document patterns** for team
6. **Write tests** as we go

---

## Questions for Discussion

1. **Should we refactor or mock?** (Recommendation: Refactor)
2. **Which pattern for which components?** (Analysis above)
3. **How much effort can we invest?** (~12-20 hours for all)
4. **Should we do all at once or incrementally?** (Incremental recommended)
5. **Who owns the facades?** (Shared between API and Client teams?)

---

**Status:** Awaiting approval to proceed with refactoring
**Recommendation:** Refactor for testability (better long-term solution)
**Next Action:** Start Phase 2 if approved
