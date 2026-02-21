using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Chronicis.Client.Models;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;

namespace Chronicis.Client.Components.Shared;

[ExcludeFromCodeCoverage]
public partial class ExternalLinkDetailPanel : ComponentBase
{
    [Parameter] public ExternalLinkContentDto? Content { get; set; }

    /// <summary>
    /// When provided, bypasses the render definition service and uses this definition directly.
    /// Used by the admin render definition generator for live preview.
    /// </summary>
    [Parameter] public RenderDefinition? DefinitionOverride { get; set; }

    private bool _useStructuredRendering;
    private bool _isLoading;
    private JsonElement? _parsedJson;
    private RenderDefinition? _definition;
    private string? _lastContentId;
    private RenderDefinition? _lastDefinitionOverride;

    protected override async Task OnParametersSetAsync()
    {
        var contentId = Content?.Id;
        var overrideChanged = DefinitionOverride != _lastDefinitionOverride;

        if (contentId == _lastContentId && !overrideChanged)
            return;
        _lastContentId = contentId;
        _lastDefinitionOverride = DefinitionOverride;

        if (Content == null || string.IsNullOrWhiteSpace(Content.JsonData))
        {
            _useStructuredRendering = false;
            _isLoading = false;
            _parsedJson = null;
            _definition = null;
            return;
        }

        try
        {
            _isLoading = true;

            var doc = JsonDocument.Parse(Content.JsonData);
            var parsedJson = doc.RootElement.Clone();

            var lastSlash = Content.Id.LastIndexOf('/');
            var categoryPath = lastSlash > 0 ? Content.Id[..lastSlash] : null;

            Logger.LogInformation(
                "ExternalLinkDetailPanel: Parsed JSON for {Id}. Category={Category}, RootKind={Kind}",
                Content.Id, categoryPath, parsedJson.ValueKind);

            var definition = DefinitionOverride
                ?? await RenderDefinitionService.ResolveAsync(Content.Source, categoryPath);

            _parsedJson = parsedJson;
            _definition = definition;
            _useStructuredRendering = true;
            _isLoading = false;

            Logger.LogInformation(
                "ExternalLinkDetailPanel: Rendering with CatchAll={CatchAll}, Sections={Sections}",
                _definition.CatchAll, _definition.Sections.Count);

            StateHasChanged();
        }
        catch (JsonException ex)
        {
            Logger.LogWarning(ex, "Failed to parse JsonData for {Id}, falling back to markdown", Content.Id);
            _useStructuredRendering = false;
            _isLoading = false;
            StateHasChanged();
        }
    }

    // ==================================================================================
    // Rendering Methods
    // ==================================================================================

    /// <summary>
    /// Gets the 'fields' object from the root, which is the primary data container.
    /// </summary>
    private static JsonElement? GetFieldsElement(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("fields", out var fields) &&
            fields.ValueKind == JsonValueKind.Object)
        {
            return fields;
        }
        return null;
    }

    /// <summary>
    /// Main render method — dispatches to definition-driven or generic rendering.
    /// </summary>
    private RenderFragment RenderStructured(JsonElement root, RenderDefinition definition) => builder =>
    {
        var fields = GetFieldsElement(root);
        var dataSource = fields ?? root;

        var renderedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Always mark hidden fields as rendered so they don't appear in catch-all
        foreach (var hidden in definition.Hidden)
        {
            renderedPaths.Add(hidden);
        }
        // Title field is rendered by the drawer header, not the panel
        renderedPaths.Add(definition.TitleField);

        var seq = 0;

        // Render defined sections
        foreach (var section in definition.Sections)
        {
            RenderSection(builder, ref seq, dataSource, section, renderedPaths);
        }

        // Catch-all: render any remaining fields not covered by sections
        if (definition.CatchAll)
        {
            RenderCatchAll(builder, ref seq, dataSource, renderedPaths);
        }
    };

    /// <summary>
    /// Renders a single defined section as an expansion panel.
    /// Skips sections where no fields would be visible (all null/empty with omitNull).
    /// </summary>
    private void RenderSection(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement dataSource,
        RenderSection section,
        HashSet<string> renderedPaths)
    {
        // If the section targets a specific path (e.g., an array), resolve it
        JsonElement sectionData = dataSource;
        if (!string.IsNullOrWhiteSpace(section.Path))
        {
            if (!dataSource.TryGetProperty(section.Path, out var pathElement))
                return; // Path doesn't exist in data — skip section

            sectionData = pathElement;
            renderedPaths.Add(section.Path);
        }

        // Pre-check: would any fields actually render?
        // If all fields are null/empty and omitNull, skip the entire section.
        if (section.Fields != null && section.Fields.Count > 0 && section.Render != "stat-row")
        {
            var hasVisibleField = false;
            foreach (var field in section.Fields)
            {
                // Multi-path: check if any path has a value
                if (field.Paths.Count > 1)
                {
                    if (field.Paths.Any(p => sectionData.TryGetProperty(p, out var v) && !IsNullOrEmpty(v)))
                    {
                        hasVisibleField = true;
                        break;
                    }
                    continue;
                }

                if (!sectionData.TryGetProperty(field.Path, out var val))
                    continue;
                if (field.OmitNull && IsNullOrEmpty(val))
                    continue;
                hasVisibleField = true;
                break;
            }
            if (!hasVisibleField)
            {
                // Still track paths as rendered so they don't leak into catch-all
                foreach (var field in section.Fields)
                    foreach (var p in field.Paths)
                        renderedPaths.Add(p);
                return;
            }
        }

        builder.OpenElement(seq++, "details");
        builder.AddAttribute(seq++, "class", "elp-section");
        if (!section.Collapsed)
        {
            builder.AddAttribute(seq++, "open", true);
        }

        // Summary (section header)
        builder.OpenElement(seq++, "summary");
        builder.AddAttribute(seq++, "class", "elp-section-header");
        builder.AddContent(seq++, section.Label);
        builder.CloseElement(); // summary

        // Section content
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "elp-section-body");

        if (section.Render == "stat-row" && section.Fields != null)
        {
            RenderStatRow(builder, ref seq, sectionData, section.Fields);
            foreach (var field in section.Fields)
                renderedPaths.Add(field.Path);
        }
        else if (section.Render == "list" && sectionData.ValueKind == JsonValueKind.Array)
        {
            RenderArrayAsList(builder, ref seq, sectionData, section.ItemFields);
        }
        else if (section.Render == "table" && sectionData.ValueKind == JsonValueKind.Array)
        {
            RenderArrayAsTable(builder, ref seq, sectionData, section.ItemFields);
        }
        else if (section.Fields != null)
        {
            foreach (var field in section.Fields)
            {
                RenderDefinedField(builder, ref seq, sectionData, field);
                foreach (var p in field.Paths)
                    renderedPaths.Add(p);
            }
        }

        builder.CloseElement(); // div.elp-section-body
        builder.CloseElement(); // details.elp-section
    }

    /// <summary>
    /// Renders all fields not already covered by defined sections.
    /// Auto-discovers structure: nested objects with 'fields' become subsections.
    /// </summary>
    private void RenderCatchAll(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement dataSource,
        HashSet<string> renderedPaths)
    {
        if (dataSource.ValueKind != JsonValueKind.Object)
            return;

        var unrenderedFields = new List<(string Name, JsonElement Value)>();
        foreach (var prop in dataSource.EnumerateObject())
        {
            if (renderedPaths.Contains(prop.Name))
                continue;
            unrenderedFields.Add((prop.Name, prop.Value));
        }

        if (unrenderedFields.Count == 0)
            return;

        // Separate scalar fields from complex ones (objects/arrays)
        var scalarFields = new List<(string Name, JsonElement Value)>();
        var complexFields = new List<(string Name, JsonElement Value)>();

        foreach (var (name, value) in unrenderedFields)
        {
            if (value.ValueKind == JsonValueKind.Object || value.ValueKind == JsonValueKind.Array)
                complexFields.Add((name, value));
            else
                scalarFields.Add((name, value));
        }

        // Render scalars as a simple properties section
        if (scalarFields.Count > 0)
        {
            builder.OpenElement(seq++, "details");
            builder.AddAttribute(seq++, "class", "elp-section");
            builder.AddAttribute(seq++, "open", true);

            builder.OpenElement(seq++, "summary");
            builder.AddAttribute(seq++, "class", "elp-section-header");
            builder.AddContent(seq++, "Properties");
            builder.CloseElement();

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "elp-section-body");

            foreach (var (name, value) in scalarFields)
            {
                // Skip null/empty values in catch-all to reduce noise
                if (IsNullOrEmpty(value))
                    continue;
                RenderKeyValue(builder, ref seq, FormatFieldName(name), FormatScalarValue(value));
            }

            builder.CloseElement(); // div
            builder.CloseElement(); // details
        }

        // Render complex fields as individual subsections
        foreach (var (name, value) in complexFields)
        {
            builder.OpenElement(seq++, "details");
            builder.AddAttribute(seq++, "class", "elp-section");
            builder.AddAttribute(seq++, "open", true);

            builder.OpenElement(seq++, "summary");
            builder.AddAttribute(seq++, "class", "elp-section-header");
            builder.AddContent(seq++, FormatFieldName(name));
            builder.CloseElement();

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "elp-section-body");

            RenderGenericValue(builder, ref seq, value, 0);

            builder.CloseElement(); // div
            builder.CloseElement(); // details
        }
    }

    // ==================================================================================
    // Field Rendering Helpers
    // ==================================================================================

    /// <summary>
    /// Renders a field defined in a RenderField specification.
    /// </summary>
    private void RenderDefinedField(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement dataSource,
        RenderField field)
    {
        if (field.Render == "hidden")
            return;

        // Multi-path support: resolve all paths and concatenate values
        if (field.Paths.Count > 1)
        {
            var parts = new List<string>();
            foreach (var path in field.Paths)
            {
                if (dataSource.TryGetProperty(path, out var partValue) && !IsNullOrEmpty(partValue))
                    parts.Add(FormatScalarValue(partValue));
            }

            if (parts.Count == 0 && field.OmitNull)
                return;

            var label = field.Label ?? FormatFieldName(field.Paths[0]);
            var combined = parts.Count > 0 ? string.Join(" ", parts) : "—";
            RenderKeyValue(builder, ref seq, label, combined);
            return;
        }

        // Single-path rendering (original behavior)
        if (!dataSource.TryGetProperty(field.Path, out var value))
            return;

        // OmitNull: skip fields with null/empty/dash values
        if (field.OmitNull && IsNullOrEmpty(value))
            return;

        var fieldLabel = field.Label ?? FormatFieldName(field.Path);

        switch (field.Render)
        {
            case "heading":
                builder.OpenElement(seq++, "h4");
                builder.AddAttribute(seq++, "class", "elp-subheading");
                builder.AddContent(seq++, FormatScalarValue(value));
                builder.CloseElement();
                break;

            case "richtext":
                var text = value.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    builder.OpenElement(seq++, "div");
                    builder.AddAttribute(seq++, "class", "elp-richtext");
                    builder.AddMarkupContent(seq++, MarkdownService.ToHtml(text));
                    builder.CloseElement();
                }
                break;

            case "chips" when value.ValueKind == JsonValueKind.Array:
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "elp-field");

                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "elp-field-label");
                builder.AddContent(seq++, fieldLabel);
                builder.CloseElement();

                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "elp-chips");
                foreach (var item in value.EnumerateArray())
                {
                    builder.OpenElement(seq++, "span");
                    builder.AddAttribute(seq++, "class", "elp-chip");
                    builder.AddContent(seq++, FormatScalarValue(item));
                    builder.CloseElement();
                }
                builder.CloseElement(); // div.elp-chips
                builder.CloseElement(); // div.elp-field
                break;

            default:
                // Default: key-value pair
                if (value.ValueKind == JsonValueKind.Object || value.ValueKind == JsonValueKind.Array)
                {
                    RenderGenericValue(builder, ref seq, value, 0);
                }
                else
                {
                    RenderKeyValue(builder, ref seq, fieldLabel, FormatScalarValue(value));
                }
                break;
        }
    }

    /// <summary>
    /// Renders an array as a list of items, each rendered with itemFields or generically.
    /// </summary>
    private void RenderArrayAsList(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement array,
        List<RenderField>? itemFields)
    {
        foreach (var item in array.EnumerateArray())
        {
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "elp-list-item");

            if (item.ValueKind == JsonValueKind.Object)
            {
                // Check for nested fields pattern
                var innerData = GetFieldsElement(item) ?? item;

                if (itemFields != null && itemFields.Count > 0)
                {
                    foreach (var field in itemFields)
                    {
                        RenderDefinedField(builder, ref seq, innerData, field);
                    }
                }
                else
                {
                    RenderGenericObject(builder, ref seq, innerData, 0);
                }
            }
            else
            {
                builder.OpenElement(seq++, "span");
                builder.AddContent(seq++, FormatScalarValue(item));
                builder.CloseElement();
            }

            builder.CloseElement(); // div.elp-list-item
        }
    }

    /// <summary>
    /// Renders an array as a table (for flat, uniform arrays).
    /// </summary>
    private void RenderArrayAsTable(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement array,
        List<RenderField>? itemFields)
    {
        // Determine columns from itemFields or first array item
        var columns = new List<(string Path, string Label)>();
        if (itemFields != null && itemFields.Count > 0)
        {
            columns = itemFields
                .Where(f => f.Render != "hidden")
                .Select(f => (f.Path, Label: f.Label ?? FormatFieldName(f.Path)))
                .ToList();
        }
        else
        {
            // Auto-detect from first item
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    var source = GetFieldsElement(item) ?? item;
                    foreach (var prop in source.EnumerateObject())
                    {
                        if (prop.Value.ValueKind != JsonValueKind.Object &&
                            prop.Value.ValueKind != JsonValueKind.Array)
                        {
                            columns.Add((prop.Name, FormatFieldName(prop.Name)));
                        }
                    }
                }
                break; // Only inspect first item
            }
        }

        if (columns.Count == 0)
            return;

        builder.OpenElement(seq++, "table");
        builder.AddAttribute(seq++, "class", "elp-table");

        // Header row
        builder.OpenElement(seq++, "thead");
        builder.OpenElement(seq++, "tr");
        foreach (var (_, label) in columns)
        {
            builder.OpenElement(seq++, "th");
            builder.AddContent(seq++, label);
            builder.CloseElement();
        }
        builder.CloseElement(); // tr
        builder.CloseElement(); // thead

        // Data rows
        builder.OpenElement(seq++, "tbody");
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            var source = GetFieldsElement(item) ?? item;

            builder.OpenElement(seq++, "tr");
            foreach (var (path, _) in columns)
            {
                builder.OpenElement(seq++, "td");
                if (source.TryGetProperty(path, out var cellValue))
                {
                    builder.AddContent(seq++, FormatScalarValue(cellValue));
                }
                builder.CloseElement();
            }
            builder.CloseElement(); // tr
        }
        builder.CloseElement(); // tbody
        builder.CloseElement(); // table
    }

    // ==================================================================================
    // Generic Rendering (no definition)
    // ==================================================================================

    /// <summary>
    /// Renders any JSON value generically. Handles recursive structure.
    /// </summary>
    private void RenderGenericValue(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement value,
        int depth)
    {
        if (depth > 10)
            return; // Safety limit

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                // Check for the { fields: {...} } pattern — unwrap it
                var inner = GetFieldsElement(value) ?? value;
                RenderGenericObject(builder, ref seq, inner, depth);
                break;

            case JsonValueKind.Array:
                RenderGenericArray(builder, ref seq, value, depth);
                break;

            default:
                builder.OpenElement(seq++, "span");
                builder.AddContent(seq++, FormatScalarValue(value));
                builder.CloseElement();
                break;
        }
    }

    /// <summary>
    /// Renders an object's properties as key-value pairs or nested subsections.
    /// </summary>
    private void RenderGenericObject(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement obj,
        int depth)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Null)
                continue;

            var label = FormatFieldName(prop.Name);

            if (prop.Value.ValueKind == JsonValueKind.Object ||
                prop.Value.ValueKind == JsonValueKind.Array)
            {
                // Nested complex value — render as a collapsible subsection
                builder.OpenElement(seq++, "details");
                builder.AddAttribute(seq++, "class", "elp-subsection");
                builder.AddAttribute(seq++, "open", true);

                builder.OpenElement(seq++, "summary");
                builder.AddAttribute(seq++, "class", "elp-subsection-header");
                builder.AddContent(seq++, label);
                builder.CloseElement();

                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "elp-subsection-body");
                RenderGenericValue(builder, ref seq, prop.Value, depth + 1);
                builder.CloseElement();

                builder.CloseElement(); // details
            }
            else
            {
                RenderKeyValue(builder, ref seq, label, FormatScalarValue(prop.Value));
            }
        }
    }

    /// <summary>
    /// Renders an array generically — simple values as a comma list,
    /// objects as individual items with dividers.
    /// </summary>
    private void RenderGenericArray(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement array,
        int depth)
    {
        // Check if all items are scalars
        var allScalar = true;
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
            {
                allScalar = false;
                break;
            }
        }

        if (allScalar)
        {
            // Render as chips/tags
            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "elp-chips");
            foreach (var item in array.EnumerateArray())
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "elp-chip");
                builder.AddContent(seq++, FormatScalarValue(item));
                builder.CloseElement();
            }
            builder.CloseElement();
        }
        else
        {
            // Render each item as a block
            foreach (var item in array.EnumerateArray())
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "class", "elp-list-item");
                RenderGenericValue(builder, ref seq, item, depth + 1);
                builder.CloseElement();
            }
        }
    }

    // ==================================================================================
    // Primitives
    // ==================================================================================

    /// <summary>
    /// Renders a simple label: value pair.
    /// Values longer than 50 characters stack below the label for readability.
    /// </summary>
    private static void RenderKeyValue(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        string label,
        string value)
    {
        var isBlock = value.Length > 50;
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", isBlock ? "elp-field elp-field--block" : "elp-field");

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "elp-field-label");
        builder.AddContent(seq++, label);
        builder.CloseElement();

        builder.OpenElement(seq++, "span");
        builder.AddAttribute(seq++, "class", "elp-field-value");
        builder.AddContent(seq++, value);
        builder.CloseElement();

        builder.CloseElement(); // div.elp-field
    }

    // ==================================================================================
    // Formatting Utilities
    // ==================================================================================

    /// <summary>
    /// Resolves the display value for a field, handling both single-path and multi-path fields.
    /// For multi-path, concatenates non-null values with a space separator.
    /// Returns (resolvedValue, found). When not found, resolvedValue is default.
    /// </summary>
    private static (JsonElement value, bool found) ResolveFieldValue(JsonElement dataSource, RenderField field)
    {
        if (field.Paths.Count <= 1)
        {
            // Single path — standard lookup
            if (dataSource.TryGetProperty(field.Path, out var val))
                return (val, true);
            return (default, false);
        }

        // Multi-path: concatenate non-null text values
        var parts = new List<string>();
        foreach (var path in field.Paths)
        {
            if (dataSource.TryGetProperty(path, out var val) && !IsNullOrEmpty(val))
                parts.Add(FormatScalarValue(val));
        }

        if (parts.Count == 0)
            return (default, false);

        // Return as a synthetic string JsonElement via round-trip
        var combined = string.Join(" ", parts);
        using var doc = JsonDocument.Parse($"\"{combined.Replace("\"", "\\\"")}\"");
        return (doc.RootElement.Clone(), true);
    }

    /// <summary>
    /// Returns all JSON field paths referenced by a RenderField (for rendered-path tracking).
    /// </summary>
    private static IEnumerable<string> GetAllPaths(RenderField field) => field.Paths;

    /// <summary>
    /// Formats a JSON field name for display: snake_case/camelCase → Title Case.
    /// </summary>
    private static string FormatFieldName(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return fieldName;

        // Replace underscores with spaces
        var withSpaces = fieldName.Replace('_', ' ');

        // Insert spaces before capitals (camelCase)
        var chars = new List<char>();
        for (int i = 0; i < withSpaces.Length; i++)
        {
            if (i > 0 && char.IsUpper(withSpaces[i]) && char.IsLower(withSpaces[i - 1]))
                chars.Add(' ');
            chars.Add(withSpaces[i]);
        }

        // Title case
        var words = new string(chars.ToArray())
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var titleCased = words.Select(w =>
            w.Length > 0
                ? char.ToUpper(w[0], CultureInfo.InvariantCulture) + w[1..].ToLower(CultureInfo.InvariantCulture)
                : w);

        return string.Join(" ", titleCased);
    }

    /// <summary>
    /// Formats a scalar JSON value for display.
    /// </summary>
    private static string FormatScalarValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Null => "—",
            _ => value.GetRawText()
        };
    }

    /// <summary>
    /// Returns true if the JSON value is null, empty string, or the em-dash placeholder.
    /// </summary>
    private static bool IsNullOrEmpty(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            return true;

        if (value.ValueKind == JsonValueKind.String)
        {
            var str = value.GetString();
            return string.IsNullOrWhiteSpace(str) || str == "—" || str == "-";
        }

        if (value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0)
            return true;

        return false;
    }

    /// <summary>
    /// Renders fields as a compact horizontal stat row (labels on top, values below).
    /// Used for D&amp;D ability scores and similar compact stat groups.
    /// </summary>
    private static void RenderStatRow(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder,
        ref int seq,
        JsonElement dataSource,
        List<RenderField> fields)
    {
        builder.OpenElement(seq++, "table");
        builder.AddAttribute(seq++, "class", "elp-stat-row");

        // Header row: labels
        builder.OpenElement(seq++, "thead");
        builder.OpenElement(seq++, "tr");
        foreach (var field in fields)
        {
            builder.OpenElement(seq++, "th");
            builder.AddContent(seq++, field.Label ?? FormatFieldName(field.Path));
            builder.CloseElement();
        }
        builder.CloseElement(); // tr
        builder.CloseElement(); // thead

        // Value row
        builder.OpenElement(seq++, "tbody");
        builder.OpenElement(seq++, "tr");
        foreach (var field in fields)
        {
            builder.OpenElement(seq++, "td");
            if (dataSource.TryGetProperty(field.Path, out var value))
            {
                builder.AddContent(seq++, FormatScalarValue(value));
            }
            else
            {
                builder.AddContent(seq++, "—");
            }
            builder.CloseElement();
        }
        builder.CloseElement(); // tr
        builder.CloseElement(); // tbody

        builder.CloseElement(); // table
    }
}
