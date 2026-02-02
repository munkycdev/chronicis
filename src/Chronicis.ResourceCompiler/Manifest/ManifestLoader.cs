using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Warnings;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Chronicis.ResourceCompiler.Manifest;

public sealed class ManifestLoader
{
    public async Task<ManifestLoadResult> LoadAsync(string manifestPath, CancellationToken cancellationToken)
    {
        var warnings = new List<Warning>();

        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            warnings.Add(new Warning(
                WarningCode.InvalidManifest,
                WarningSeverity.Error,
                "Manifest path is required."));
            return new ManifestLoadResult(null, warnings);
        }

        if (!File.Exists(manifestPath))
        {
            warnings.Add(new Warning(
                WarningCode.InvalidManifest,
                WarningSeverity.Error,
                $"Manifest file not found: {manifestPath}"));
            return new ManifestLoadResult(null, warnings);
        }

        string yaml;
        try
        {
            yaml = await File.ReadAllTextAsync(manifestPath, cancellationToken);
        }
        catch (Exception ex)
        {
            warnings.Add(new Warning(
                WarningCode.InvalidManifest,
                WarningSeverity.Error,
                $"Failed to read manifest file: {ex.Message}"));
            return new ManifestLoadResult(null, warnings);
        }

        ManifestDto? dto;
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            dto = deserializer.Deserialize<ManifestDto>(yaml);
        }
        catch (YamlException ex)
        {
            warnings.Add(new Warning(
                WarningCode.InvalidManifest,
                WarningSeverity.Error,
                $"Failed to parse manifest YAML: {ex.Message}"));
            return new ManifestLoadResult(null, warnings);
        }

        var manifest = MapManifest(dto);
        return new ManifestLoadResult(manifest, warnings);
    }

    private static Models.Manifest MapManifest(ManifestDto? dto)
    {
        var entities = new Dictionary<string, ManifestEntity>();

        if (dto?.Entities is null)
        {
            return new Models.Manifest { Entities = entities };
        }

        foreach (var pair in dto.Entities)
        {
            var entityName = pair.Key ?? string.Empty;
            var entityDto = pair.Value ?? new ManifestEntityDto();

            if (entities.ContainsKey(entityName))
            {
                continue;
            }

            var entity = new ManifestEntity
            {
                Name = entityName,
                File = entityDto.File ?? string.Empty,
                PrimaryKey = entityDto.Pk ?? string.Empty,
                IsRoot = entityDto.Root ?? false,
                OrderBy = MapOrderBy(entityDto.OrderBy),
                Children = MapChildren(entityDto.Children)
            };

            entities[entityName] = entity;
        }

        return new Models.Manifest { Entities = entities };
    }

    private static IReadOnlyList<ManifestChild> MapChildren(IReadOnlyList<ManifestChildDto>? children)
    {
        if (children is null || children.Count == 0)
        {
            return Array.Empty<ManifestChild>();
        }

        var result = new List<ManifestChild>(children.Count);
        foreach (var child in children)
        {
            var mapped = new ManifestChild
            {
                Entity = child.Entity ?? string.Empty,
                As = child.As ?? string.Empty,
                ForeignKeyField = child.Fk?.Field ?? string.Empty,
                OrderBy = MapOrderBy(child.OrderBy),
                MaxDepth = child.MaxDepth
            };
            result.Add(mapped);
        }

        return result;
    }

    private static ManifestOrderBy? MapOrderBy(ManifestOrderByDto? orderBy)
    {
        if (orderBy is null)
        {
            return null;
        }

        return new ManifestOrderBy
        {
            Field = orderBy.Field ?? string.Empty,
            Direction = ParseDirection(orderBy.Direction)
        };
    }

    private static ManifestOrderByDirection? ParseDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
        {
            return null;
        }

        if (direction.Equals("asc", StringComparison.OrdinalIgnoreCase))
        {
            return ManifestOrderByDirection.Asc;
        }

        if (direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            return ManifestOrderByDirection.Desc;
        }

        return null;
    }

    private sealed class ManifestDto
    {
        public Dictionary<string, ManifestEntityDto>? Entities { get; set; }
    }

    private sealed class ManifestEntityDto
    {
        public string? File { get; set; }
        public string? Pk { get; set; }
        public bool? Root { get; set; }
        public List<ManifestChildDto>? Children { get; set; }
        public ManifestOrderByDto? OrderBy { get; set; }
    }

    private sealed class ManifestChildDto
    {
        public string? Entity { get; set; }
        [YamlMember(Alias = "as")]
        public string? As { get; set; }
        public ManifestForeignKeyDto? Fk { get; set; }
        public ManifestOrderByDto? OrderBy { get; set; }
        public int? MaxDepth { get; set; }
    }

    private sealed class ManifestForeignKeyDto
    {
        public string? Field { get; set; }
    }

    private sealed class ManifestOrderByDto
    {
        public string? Field { get; set; }
        public string? Direction { get; set; }
    }
}
