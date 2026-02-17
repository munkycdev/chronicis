using System.Diagnostics.CodeAnalysis;

namespace Chronicis.ResourceCompiler.Indexing.Models;

[ExcludeFromCodeCoverage]
public readonly record struct KeyValue(KeyKind Kind, string CanonicalValue);
