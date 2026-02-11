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
    [Inject] private ILogger<RenderDefinitionGenerator> Logger { get; set; } = default!;

    private string _selectedSource = "ros";
    private string _recordId = "";
    private bool _isLoading;
    private string? _loadError;

    private ExternalLinkContentDto? _sampleContent;
    private string _definitionJson = "";
    private string? _jsonError;
    private bool _definitionDirty;
    private RenderDefinition? _activeDefinition;

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
            // For "bestiary/beast/garoug" -> "render-definitions/ros/bestiary.json"
            // For "spells/1st-level/magic-missile" -> "render-definitions/ros/spells.json"
            var category = parts.Length > 0 ? parts[0] : "unknown";
            return $"wwwroot/render-definitions/{_selectedSource}/{category}.json";
        }
    }

    private async Task OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
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

            // Apply immediately to preview
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

        // Validate JSON on each change
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
