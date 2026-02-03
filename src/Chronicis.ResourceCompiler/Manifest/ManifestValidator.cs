using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Manifest;

public sealed class ManifestValidator
{
    public IReadOnlyList<Warning> Validate(Models.Manifest manifest)
    {
        var warnings = new List<Warning>();

        if (manifest.Entities is null || manifest.Entities.Count == 0)
        {
            warnings.Add(new Warning(
                WarningCode.InvalidManifest,
                WarningSeverity.Error,
                "Manifest must define at least one entity."));
            return warnings;
        }

        var entityNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entityLookup = new HashSet<string>(manifest.Entities.Keys, StringComparer.OrdinalIgnoreCase);

        foreach (var pair in manifest.Entities)
        {
            var entityName = pair.Key;
            var entity = pair.Value;

            if (string.IsNullOrWhiteSpace(entityName))
            {
                warnings.Add(new Warning(
                    WarningCode.InvalidManifest,
                    WarningSeverity.Error,
                    "Entity name cannot be empty."));
            }
            else if (!entityNameSet.Add(entityName))
            {
                warnings.Add(new Warning(
                    WarningCode.InvalidManifest,
                    WarningSeverity.Error,
                    $"Duplicate entity name: {entityName}"));
            }

            if (string.IsNullOrWhiteSpace(entity.File))
            {
                warnings.Add(new Warning(
                    WarningCode.MissingKey,
                    WarningSeverity.Error,
                    $"Entity '{entityName}' must define a non-empty file name.",
                    entityName));
            }

            if (string.IsNullOrWhiteSpace(entity.PrimaryKey))
            {
                warnings.Add(new Warning(
                    WarningCode.MissingKey,
                    WarningSeverity.Error,
                    $"Entity '{entityName}' must define a non-empty primary key.",
                    entityName));
            }

            ValidateOrderBy(entityName, entity.OrderBy, warnings);

            if (entity.IsRoot)
            {
                if (entity.Output is null || string.IsNullOrWhiteSpace(entity.Output.BlobTemplate))
                {
                    warnings.Add(new Warning(
                        WarningCode.InvalidManifest,
                        WarningSeverity.Error,
                        $"Root entity '{entityName}' must define output.blobTemplate.",
                        entityName));
                }

                if (entity.Output?.Index is not null)
                {
                    if (string.IsNullOrWhiteSpace(entity.Output.Index.Blob))
                    {
                        warnings.Add(new Warning(
                            WarningCode.InvalidManifest,
                            WarningSeverity.Error,
                            $"Entity '{entityName}' output.index.blob is required when index is defined.",
                            entityName));
                    }

                    if (entity.Output.Index.Fields is null || entity.Output.Index.Fields.Count == 0)
                    {
                        warnings.Add(new Warning(
                            WarningCode.InvalidManifest,
                            WarningSeverity.Error,
                            $"Entity '{entityName}' output.index.fields must define at least one field.",
                            entityName));
                    }
                }

                if (RequiresSlug(entity.Output?.BlobTemplate, entity.Output?.Index?.Blob))
                {
                    if (entity.Identity is null || string.IsNullOrWhiteSpace(entity.Identity.SlugField))
                    {
                        warnings.Add(new Warning(
                            WarningCode.InvalidManifest,
                            WarningSeverity.Error,
                            $"Entity '{entityName}' requires identity.slugField for template token resolution.",
                            entityName));
                    }
                }
            }

            foreach (var child in entity.Children)
            {
                if (string.IsNullOrWhiteSpace(child.Entity))
                {
                    warnings.Add(new Warning(
                        WarningCode.InvalidManifest,
                        WarningSeverity.Error,
                        $"Entity '{entityName}' has a child relationship with no entity specified.",
                        entityName));
                }
                else if (!entityLookup.Contains(child.Entity))
                {
                    warnings.Add(new Warning(
                        WarningCode.InvalidManifest,
                        WarningSeverity.Error,
                        $"Entity '{entityName}' references missing child entity '{child.Entity}'.",
                        entityName));
                }

                if (string.IsNullOrWhiteSpace(child.ForeignKeyField))
                {
                    warnings.Add(new Warning(
                        WarningCode.MissingForeignKey,
                        WarningSeverity.Error,
                        $"Entity '{entityName}' must define a foreign key field for child '{child.Entity}'.",
                        entityName));
                }

                var resolvedName = string.IsNullOrWhiteSpace(child.As) ? child.Entity : child.As;
                if (string.IsNullOrWhiteSpace(resolvedName))
                {
                    warnings.Add(new Warning(
                        WarningCode.InvalidManifest,
                        WarningSeverity.Error,
                        $"Entity '{entityName}' defines a child relationship with no resolved name.",
                        entityName));
                }

                ValidateOrderBy(entityName, child.OrderBy, warnings);
            }
        }

        return warnings;
    }

    private static bool RequiresSlug(string? blobTemplate, string? indexBlob)
    {
        return (blobTemplate?.Contains("{slug}", StringComparison.OrdinalIgnoreCase) ?? false)
               || (indexBlob?.Contains("{slug}", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static void ValidateOrderBy(string entityName, Models.ManifestOrderBy? orderBy, List<Warning> warnings)
    {
        if (orderBy is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(orderBy.Field))
        {
            warnings.Add(new Warning(
                WarningCode.OrderByFieldMissing,
                WarningSeverity.Error,
                $"Entity '{entityName}' defines orderBy without a field.",
                entityName));
        }

        if (orderBy.Direction is null)
        {
            warnings.Add(new Warning(
                WarningCode.InvalidManifest,
                WarningSeverity.Error,
                $"Entity '{entityName}' defines orderBy with an invalid or missing direction.",
                entityName));
        }
    }
}
