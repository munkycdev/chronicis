using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;

namespace Chronicis.Client.ViewModels;

/// <summary>
/// ViewModel for AISummarySection component.
/// Contains all business logic for AI summary generation, separated from UI rendering.
/// </summary>
public class AISummarySectionViewModel : IDisposable
{
    private readonly IAISummaryFacade _facade;

    // State fields
    private Guid _entityId;
    private string _entityType = "Article";
    private ArticleSummaryDto? _articleSummaryData;
    private EntitySummaryDto? _entitySummaryData;
    private SummaryEstimateDto? _estimate;
    private List<SummaryTemplateDto> _templates = new();
    private bool _isLoading = false;
    private string _loadingMessage = "";
    private string _errorMessage = "";
    private Guid? _selectedTemplateId;
    private string? _customPrompt;
    private bool _saveConfiguration = false;
    private bool _advancedExpanded = false;
    private bool _isExpanded = false;

    // Events
    public event Action? StateChanged;

    // Public properties (read-only state exposure)
    public bool IsLoading => _isLoading;
    public string LoadingMessage => _loadingMessage;
    public string ErrorMessage => _errorMessage;
    public bool IsExpanded => _isExpanded;
    public bool AdvancedExpanded => _advancedExpanded;
    public SummaryEstimateDto? Estimate => _estimate;
    public List<SummaryTemplateDto> Templates => _templates;
    public Guid? SelectedTemplateId => _selectedTemplateId;
    public string? CustomPrompt => _customPrompt;
    public bool SaveConfiguration => _saveConfiguration;

    // Unified summary accessor
    private SummaryDataAccessor? _summaryData => _entityType == "Article" 
        ? (_articleSummaryData != null ? new SummaryDataAccessor(_articleSummaryData) : null)
        : (_entitySummaryData != null ? new SummaryDataAccessor(_entitySummaryData) : null);

    public bool HasSummary => _summaryData?.HasSummary == true;
    public string? Summary => _summaryData?.Summary;
    public DateTime? GeneratedAt => _summaryData?.GeneratedAt;
    public string? TemplateName => _summaryData?.TemplateName;

    // Computed properties based on entity type
    public string NoSourcesMessage => _entityType switch
    {
        "Article" => "This article has no content and isn't referenced by other articles. Add content or create wiki links like [[Article Name]] to generate a summary.",
        "Campaign" => "No session notes found. Add sessions to your arcs to generate a campaign summary.",
        "Arc" => "No session notes found in this arc yet.",
        _ => "No sources available for summary generation."
    };

    public string SourceLabel => _entityType == "Article" ? "source" : "session";
    public string RememberSettingsLabel => $"Remember settings for this {_entityType.ToLower()}";
    public string CustomPromptPlaceholder => _entityType switch
    {
        "Article" => "e.g., Focus on relationships and motivations...",
        "Campaign" => "e.g., Emphasize major plot developments...",
        "Arc" => "e.g., Highlight character growth in this arc...",
        _ => "Additional instructions for the AI..."
    };

    public AISummarySectionViewModel(IAISummaryFacade facade)
    {
        _facade = facade;
    }

    public async Task InitializeAsync(Guid entityId, string entityType, bool isExpanded = false)
    {
        _entityId = entityId;
        _entityType = entityType;
        _isExpanded = isExpanded;

        // Load templates
        _templates = await _facade.GetTemplatesAsync();
        
        // Set default template if none selected
        if (_selectedTemplateId == null && _templates.Any())
        {
            _selectedTemplateId = _templates.FirstOrDefault(t => t.Name == "Default")?.Id 
                                  ?? _templates.First().Id;
        }

        // Load summary data
        await LoadSummaryDataAsync();
        
        NotifyStateChanged();
    }

    public void SetSelectedTemplateId(Guid? templateId)
    {
        _selectedTemplateId = templateId;
        NotifyStateChanged();
    }

    public void SetCustomPrompt(string? prompt)
    {
        _customPrompt = prompt;
        NotifyStateChanged();
    }

    public void SetSaveConfiguration(bool save)
    {
        _saveConfiguration = save;
        NotifyStateChanged();
    }

    public void SetAdvancedExpanded(bool expanded)
    {
        _advancedExpanded = expanded;
        NotifyStateChanged();
    }

    private async Task LoadSummaryDataAsync()
    {
        _errorMessage = "";
        
        if (_entityType == "Article")
        {
            _articleSummaryData = await _facade.GetSummaryAsync(_entityId);
            
            if (_articleSummaryData?.HasSummary == true)
            {
                _selectedTemplateId = _articleSummaryData.TemplateId;
                _customPrompt = _articleSummaryData.CustomPrompt;
            }
        }
        else
        {
            _entitySummaryData = await _facade.GetEntitySummaryAsync(_entityType, _entityId);
            
            if (_entitySummaryData?.HasSummary == true)
            {
                _selectedTemplateId = _entitySummaryData.TemplateId;
                _customPrompt = _entitySummaryData.CustomPrompt;
            }
        }
        
        NotifyStateChanged();
    }

    private async Task LoadEstimateDataAsync()
    {
        if (_summaryData?.HasSummary == true)
        {
            // Already has summary, no need for estimate
            return;
        }
        
        _isLoading = true;
        _errorMessage = "";
        NotifyStateChanged();
        
        try
        {
            if (_entityType == "Article")
            {
                _estimate = await _facade.GetEstimateAsync(_entityId);
            }
            else
            {
                _estimate = await _facade.GetEntityEstimateAsync(_entityType, _entityId);
            }
            
            LoadSettingsFromEstimate();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load estimate: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    private void LoadSettingsFromEstimate()
    {
        if (_estimate != null)
        {
            _selectedTemplateId = _estimate.TemplateId 
                ?? _templates.FirstOrDefault(t => t.Name == "Default")?.Id 
                ?? _templates.FirstOrDefault()?.Id;
            _customPrompt = _estimate.CustomPrompt;
            _advancedExpanded = !string.IsNullOrEmpty(_customPrompt);
        }
    }

    public async Task GenerateSummaryAsync()
    {
        _isLoading = true;
        _loadingMessage = _entityType == "Article" 
            ? "Reading backlinks and crafting summary..." 
            : "Analyzing session notes...";
        _errorMessage = "";
        NotifyStateChanged();

        try
        {
            var request = new GenerateSummaryRequestDto
            {
                TemplateId = _selectedTemplateId,
                CustomPrompt = string.IsNullOrWhiteSpace(_customPrompt) ? null : _customPrompt,
                SaveConfiguration = _saveConfiguration
            };

            SummaryGenerationDto? result;
            
            if (_entityType == "Article")
            {
                result = await _facade.GenerateSummaryAsync(_entityId, request);
                if (result?.Success == true)
                {
                    _articleSummaryData = new ArticleSummaryDto
                    {
                        ArticleId = _entityId,
                        Summary = result.Summary,
                        GeneratedAt = result.GeneratedDate,
                        TemplateId = _selectedTemplateId,
                        TemplateName = _templates.FirstOrDefault(t => t.Id == _selectedTemplateId)?.Name,
                        CustomPrompt = _customPrompt
                    };
                }
            }
            else
            {
                result = await _facade.GenerateEntitySummaryAsync(_entityType, _entityId, request);
                if (result?.Success == true)
                {
                    _entitySummaryData = new EntitySummaryDto
                    {
                        EntityId = _entityId,
                        EntityType = _entityType,
                        Summary = result.Summary,
                        GeneratedAt = result.GeneratedDate,
                        TemplateId = _selectedTemplateId,
                        TemplateName = _templates.FirstOrDefault(t => t.Id == _selectedTemplateId)?.Name,
                        CustomPrompt = _customPrompt
                    };
                }
            }
            
            if (result?.Success == true)
            {
                _facade.ShowSuccess("Summary generated!");
            }
            else
            {
                _errorMessage = result?.ErrorMessage ?? "Failed to generate summary";
                _facade.ShowError(_errorMessage);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            _facade.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task RegenerateSummaryAsync()
    {
        await ClearSummaryAsync();
        await GenerateSummaryAsync();
    }

    public async Task ClearSummaryAsync()
    {
        bool success;
        
        if (_entityType == "Article")
        {
            success = await _facade.ClearSummaryAsync(_entityId);
            _articleSummaryData = null;
        }
        else
        {
            success = await _facade.ClearEntitySummaryAsync(_entityType, _entityId);
            _entitySummaryData = null;
        }
        
        if (success)
        {
            await LoadSummaryDataAsync();
            _facade.ShowInfo("Summary cleared");
        }
        else
        {
            _facade.ShowError("Failed to clear summary");
        }
        
        NotifyStateChanged();
    }

    public async Task CopySummaryAsync()
    {
        var summary = _summaryData?.Summary;
        if (!string.IsNullOrEmpty(summary))
        {
            await _facade.CopyToClipboardAsync(summary);
            _facade.ShowSuccess("Copied to clipboard");
        }
    }

    public async Task ToggleExpandedAsync()
    {
        _isExpanded = !_isExpanded;
        
        if (_isExpanded)
        {
            // Panel is opening - load estimate if needed
            await LoadEstimateDataAsync();
        }
        else
        {
            // Panel is closing - clear estimate to force fresh fetch on next open
            _estimate = null;
            _errorMessage = "";
        }
        
        NotifyStateChanged();
    }

    public string GetRelativeTime(DateTime date)
    {
        var span = DateTime.UtcNow - date;
        
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        
        return date.ToLocalTime().ToString("MMM d");
    }

    public string GetEstimateTooltip()
    {
        if (_estimate == null) return "";
        return $"~{_estimate.EstimatedInputTokens:N0} input tokens, ~{_estimate.EstimatedOutputTokens:N0} output tokens";
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        // No resources to dispose currently
    }

    /// <summary>
    /// Helper class to unify access to Article and Entity summary data
    /// </summary>
    private class SummaryDataAccessor
    {
        public string? Summary { get; }
        public DateTime? GeneratedAt { get; }
        public bool HasSummary { get; }
        public string? TemplateName { get; }
        public Guid? TemplateId { get; }
        public string? CustomPrompt { get; }

        public SummaryDataAccessor(ArticleSummaryDto dto)
        {
            Summary = dto.Summary;
            GeneratedAt = dto.GeneratedAt;
            HasSummary = dto.HasSummary;
            TemplateName = dto.TemplateName;
            TemplateId = dto.TemplateId;
            CustomPrompt = dto.CustomPrompt;
        }

        public SummaryDataAccessor(EntitySummaryDto dto)
        {
            Summary = dto.Summary;
            GeneratedAt = dto.GeneratedAt;
            HasSummary = dto.HasSummary;
            TemplateName = dto.TemplateName;
            TemplateId = dto.TemplateId;
            CustomPrompt = dto.CustomPrompt;
        }
    }
}
