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

                ValidateOrderBy(entityName, child.OrderBy, warnings);
            }
        }

        return warnings;
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
