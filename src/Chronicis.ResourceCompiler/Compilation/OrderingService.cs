using System.Linq;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Manifest.Models;
using Chronicis.ResourceCompiler.Raw.Models;
using Chronicis.ResourceCompiler.Serialization;
using Chronicis.ResourceCompiler.Warnings;

namespace Chronicis.ResourceCompiler.Compilation;

public sealed class OrderingService
{
    private readonly Indexing.KeyCanonicalizer _canonicalizer = new();

    public IReadOnlyList<RawEntityRow> ApplyOrder(
        IReadOnlyList<RawEntityRow> rows,
        ManifestOrderBy? orderBy,
        string entityName,
        WarningSink warningSink)
    {
        if (orderBy is null || string.IsNullOrWhiteSpace(orderBy.Field) || orderBy.Direction is null)
        {
            return rows.OrderBy(row => row.RowIndex).ToArray();
        }

        var entries = rows.Select(row =>
        {
            if (!JsonPathAccessor.TryGetByPath(row.Data, orderBy.Field, out var element))
            {
                warningSink.Add(new Warning(
                    WarningCode.OrderByFieldMissing,
                    WarningSeverity.Warning,
                    $"Missing orderBy field '{orderBy.Field}' at row {row.RowIndex} for entity '{entityName}'.",
                    entityName,
                    JsonPathAccessor.ToJsonPath(row.RowIndex, orderBy.Field)));
                return new OrderEntry(row, false, default);
            }

            if (!_canonicalizer.TryCanonicalize(element, out var key, out _))
            {
                warningSink.Add(new Warning(
                    WarningCode.OrderByFieldMissing,
                    WarningSeverity.Warning,
                    $"Invalid orderBy field '{orderBy.Field}' at row {row.RowIndex} for entity '{entityName}'.",
                    entityName,
                    JsonPathAccessor.ToJsonPath(row.RowIndex, orderBy.Field)));
                return new OrderEntry(row, false, default);
            }

            return new OrderEntry(row, true, key);
        }).ToArray();

        var keyComparer = orderBy.Direction == ManifestOrderByDirection.Asc
            ? KeyValueComparer.Ascending
            : KeyValueComparer.Descending;

        var ordered = entries
            .OrderBy(entry => entry.HasKey ? 0 : 1)
            .ThenBy(entry => entry.Key, keyComparer)
            .ThenBy(entry => entry.Row.RowIndex)
            .Select(entry => entry.Row)
            .ToArray();

        return ordered;
    }

    private readonly record struct OrderEntry(RawEntityRow Row, bool HasKey, KeyValue Key);

    private sealed class KeyValueComparer : IComparer<KeyValue>
    {
        public static readonly KeyValueComparer Ascending = new(false);
        public static readonly KeyValueComparer Descending = new(true);

        private readonly bool _descending;

        private KeyValueComparer(bool descending)
        {
            _descending = descending;
        }

        public int Compare(KeyValue x, KeyValue y)
        {
            var kindCompare = x.Kind.CompareTo(y.Kind);
            if (kindCompare != 0)
            {
                return _descending ? -kindCompare : kindCompare;
            }

            var valueCompare = StringComparer.Ordinal.Compare(x.CanonicalValue, y.CanonicalValue);
            return _descending ? -valueCompare : valueCompare;
        }
    }
}
