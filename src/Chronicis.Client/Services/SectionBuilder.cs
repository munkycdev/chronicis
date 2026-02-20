using Chronicis.Client.Models;
using static Chronicis.Client.Services.RenderDefinitionHelpers;

namespace Chronicis.Client.Services;

/// <summary>
/// Builds RenderSection lists from classified fields and prefix groups.
/// </summary>
public static class SectionBuilder
{
    public static List<RenderSection> Build(List<FieldInfo> remaining, List<PrefixGroup> prefixGroups)
    {
        var groupedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in prefixGroups)
            foreach (var fi in group.Fields)
                groupedNames.Add(fi.Name);

        var ungrouped = remaining.Where(f => !groupedNames.Contains(f.Name)).ToList();
        var sections = new List<RenderSection>();

        AddOverviewSection(sections, ungrouped);
        AddGroupedSections(sections, prefixGroups);
        AddComplexFieldsSection(sections, ungrouped);

        return sections;
    }

    private static void AddOverviewSection(List<RenderSection> sections, List<FieldInfo> ungrouped)
    {
        var overviewFields = ungrouped
            .Where(f => !f.IsComplex)
            .OrderBy(f => IsDescriptionField(f.Name) ? 1 : 0)
            .ToList();

        if (overviewFields.Count > 0)
        {
            sections.Add(new RenderSection
            {
                Label = "Overview",
                Render = "fields",
                Fields = overviewFields.Select(f => new RenderField
                {
                    Path = f.Name,
                    Label = FormatFieldName(f.Name),
                    Render = IsDescriptionField(f.Name) ? "richtext" : "text"
                }).ToList()
            });
        }
    }

    private static void AddGroupedSections(List<RenderSection> sections, List<PrefixGroup> prefixGroups)
    {
        foreach (var group in prefixGroups.OrderBy(g => g.Label))
        {
            if (PrefixGroupDetector.IsAbilityScoreGroup(group))
            {
                sections.Add(BuildAbilityScoreSection(group));
            }
            else
            {
                var mostlyNull = group.Fields.Count(f => f.IsNull) > group.Fields.Count / 2;
                sections.Add(new RenderSection
                {
                    Label = FormatGroupLabel(group.Prefix),
                    Render = "fields",
                    Collapsed = mostlyNull,
                    Fields = group.Fields
                        .OrderBy(f => f.IsNull ? 1 : 0)
                        .Select(f => new RenderField
                        {
                            Path = f.Name,
                            Label = FormatFieldName(StripPrefix(f.Name, group.Prefix))
                        }).ToList()
                });
            }
        }
    }

    private static RenderSection BuildAbilityScoreSection(PrefixGroup group)
    {
        return new RenderSection
        {
            Label = "Ability Scores",
            Render = "stat-row",
            Fields = AbilitySuffixes.Select((suffix, i) =>
            {
                var match = group.Fields.FirstOrDefault(f =>
                    f.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
                return new RenderField
                {
                    Path = match!.Name,
                    Label = AbilityLabels[i]
                };
            }).ToList()
        };
    }

    private static void AddComplexFieldsSection(List<RenderSection> sections, List<FieldInfo> ungrouped)
    {
        var complexFields = ungrouped.Where(f => f.IsComplex).ToList();
        if (complexFields.Count > 0)
        {
            sections.Add(new RenderSection
            {
                Label = "Additional Data",
                Render = "fields",
                Collapsed = true,
                Fields = complexFields.Select(f => new RenderField
                {
                    Path = f.Name,
                    Label = FormatFieldName(f.Name)
                }).ToList()
            });
        }
    }
}
