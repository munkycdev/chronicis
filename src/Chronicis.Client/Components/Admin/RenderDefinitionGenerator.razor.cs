using System.Text.Json;
using System.Text.Json.Serialization;
using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Chronicis.Client.Components.Admin;

public partial class RenderDefinitionGenerator : ComponentBase
{
    [Inject] private IExternalLinkApiService ExternalLinkApi { get; set; } = default!;
    [Inject] private IRenderDefinitionService RenderDefService { get; set; } = default!;
    [Inject] private ILogger<RenderDefinitionGenerator> Logger { get; set; } = default!;

    private string _selectedSource = "ros";
    private string _recordId = "";
    private bool _isLoading;
    private string? _loadError;
    private string? _existingDefStatus;

    private ExternalLinkContentDto? _sampleContent;
    private string _definitionJson = "";
    private string? _jsonError;
    private bool _definitionDirty;
    private RenderDefinition? _activeDefinition;

    // Autocomplete support
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private string SuggestedFilePath
    {
        get
        {
            if (string.IsNullOrEmpty(_recordId)) return "";
            var parts = _recordId.Split('/');
            var category = parts.Length > 0 ? parts[0] : "unknown";
            return $"wwwroot/render-definitions/{_selectedSource}/{category}.json";
        }
    }

    private async Task<IEnumerable<string>> SearchRecords(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Enumerable.Empty<string>();

        try
        {
            var results = await ExternalLinkApi.GetSuggestionsAsync(
                null, _selectedSource, query, ct);
            return results.Select(r => r.Id);
        }
        catch (OperationCanceledException)
        {
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Autocomplete search failed for {Query}", query);
            return Enumerable.Empty<string>();
        }
    }

    private async Task OnRecordSelected(string? value)
    {
        _recordId = value ?? "";
        if (!string.IsNullOrWhiteSpace(_recordId))
            await LoadSampleRecord();
    }

    private async Task LoadSampleRecord()
    {
        if (string.IsNullOrWhiteSpace(_recordId))
        {
            _loadError = "Please enter a record ID.";
            return;
        }

        _isLoading = true;
        _loadError = null;
        _existingDefStatus = null;
        _sampleContent = null;
        _activeDefinition = null;
        _definitionJson = "";
        StateHasChanged();

        try
        {
            var content = await ExternalLinkApi.GetContentAsync(
                _selectedSource, _recordId.Trim(), CancellationToken.None);

            if (content == null)
            {
                _loadError = $"Record not found: {_selectedSource}/{_recordId}";
            }
            else if (string.IsNullOrWhiteSpace(content.JsonData))
            {
                _loadError = "Record loaded but has no JsonData — structured rendering unavailable.";
                _sampleContent = content;
            }
            else
            {
                _sampleContent = content;
                Logger.LogInformation("Loaded sample record: {Id} with {Len} bytes of JSON",
                    content.Id, content.JsonData.Length);

                // Try to load an existing render definition for this record's category
                await TryLoadExistingDefinition();
            }
        }
        catch (Exception ex)
        {
            _loadError = $"Failed to load record: {ex.Message}";
            Logger.LogError(ex, "Error loading sample record {Source}/{Id}", _selectedSource, _recordId);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task TryLoadExistingDefinition()
    {
        if (_sampleContent == null) return;

        try
        {
            var lastSlash = _sampleContent.Id.LastIndexOf('/');
            var categoryPath = lastSlash > 0 ? _sampleContent.Id[..lastSlash] : null;

            var existing = await RenderDefService.ResolveAsync(_sampleContent.Source, categoryPath);

            // Check if it's the built-in default or an actual file definition
            // The built-in default has no DisplayName and empty Sections
            var isDefault = existing.Sections.Count == 0 && string.IsNullOrEmpty(existing.DisplayName);

            if (!isDefault)
            {
                _definitionJson = JsonSerializer.Serialize(existing, WriteOptions);
                _activeDefinition = existing;
                _definitionDirty = false;
                _jsonError = null;
                _existingDefStatus = $"Loaded existing definition ({existing.Sections.Count} sections)";
                Logger.LogInformation("Loaded existing render definition for {Source}/{Category}",
                    _sampleContent.Source, categoryPath);
            }
            else
            {
                _existingDefStatus = "No custom definition found — use Auto-Generate to create one.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load existing render definition");
            _existingDefStatus = "Could not check for existing definition.";
        }
    }

    private void AutoGenerate()
    {
        if (_sampleContent?.JsonData == null) return;

        try
        {
            using var doc = JsonDocument.Parse(_sampleContent.JsonData);
            var definition = RenderDefinitionGeneratorService.Generate(doc.RootElement);
            _definitionJson = JsonSerializer.Serialize(definition, WriteOptions);
            _jsonError = null;
            _definitionDirty = false;

            _activeDefinition = definition;
            Logger.LogInformation("Auto-generated definition with {Sections} sections, {Hidden} hidden fields",
                definition.Sections.Count, definition.Hidden.Count);
        }
        catch (Exception ex)
        {
            _jsonError = $"Generation failed: {ex.Message}";
            Logger.LogError(ex, "Auto-generation failed");
        }
    }

    private void OnDefinitionJsonChanged(string newJson)
    {
        _definitionJson = newJson;
        _definitionDirty = true;
        _jsonError = null;

        try
        {
            JsonSerializer.Deserialize<RenderDefinition>(newJson, WriteOptions);
        }
        catch (JsonException ex)
        {
            _jsonError = $"Invalid JSON: {ex.Message}";
        }
    }

    private void ApplyDefinition()
    {
        if (string.IsNullOrWhiteSpace(_definitionJson)) return;

        try
        {
            var definition = JsonSerializer.Deserialize<RenderDefinition>(_definitionJson, WriteOptions);
            if (definition == null)
            {
                _jsonError = "Deserialized to null.";
                return;
            }

            _activeDefinition = definition;
            _definitionDirty = false;
            _jsonError = null;
            Logger.LogInformation("Applied edited definition: {Sections} sections", definition.Sections.Count);
        }
        catch (JsonException ex)
        {
            _jsonError = $"Cannot apply — invalid JSON: {ex.Message}";
        }
    }
}
