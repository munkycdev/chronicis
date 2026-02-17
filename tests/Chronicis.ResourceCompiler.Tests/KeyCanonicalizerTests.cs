using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Chronicis.ResourceCompiler.Indexing;
using Chronicis.ResourceCompiler.Indexing.Models;
using Chronicis.ResourceCompiler.Warnings;
using Xunit;

namespace Chronicis.ResourceCompiler.Tests;

[ExcludeFromCodeCoverage]
public sealed class KeyCanonicalizerTests
{
    [Fact]
    public void CanonicalizesString()
    {
        var element = JsonDocument.Parse("\"abc\"").RootElement;
        var canonicalizer = new KeyCanonicalizer();

        var success = canonicalizer.TryCanonicalize(element, out var key, out var warning);

        Assert.True(success);
        Assert.Null(warning);
        Assert.Equal(new KeyValue(KeyKind.String, "abc"), key);
    }

    [Fact]
    public void CanonicalizesBoolean()
    {
        var element = JsonDocument.Parse("true").RootElement;
        var canonicalizer = new KeyCanonicalizer();

        var success = canonicalizer.TryCanonicalize(element, out var key, out var warning);

        Assert.True(success);
        Assert.Null(warning);
        Assert.Equal(new KeyValue(KeyKind.Boolean, "true"), key);
    }

    [Fact]
    public void CanonicalizesNumericEquivalence()
    {
        var canonicalizer = new KeyCanonicalizer();
        var one = JsonDocument.Parse("1").RootElement;
        var onePointZero = JsonDocument.Parse("1.0").RootElement;
        var onePointZeroZero = JsonDocument.Parse("1.00").RootElement;

        Assert.True(canonicalizer.TryCanonicalize(one, out var key1, out _));
        Assert.True(canonicalizer.TryCanonicalize(onePointZero, out var key2, out _));
        Assert.True(canonicalizer.TryCanonicalize(onePointZeroZero, out var key3, out _));

        Assert.Equal(key1, key2);
        Assert.Equal(key2, key3);
        Assert.Equal("1", key1.CanonicalValue);
    }

    [Fact]
    public void NormalizesNegativeZero()
    {
        var canonicalizer = new KeyCanonicalizer();
        var minusZero = JsonDocument.Parse("-0").RootElement;
        var minusZeroPoint = JsonDocument.Parse("-0.0").RootElement;

        Assert.True(canonicalizer.TryCanonicalize(minusZero, out var key1, out _));
        Assert.True(canonicalizer.TryCanonicalize(minusZeroPoint, out var key2, out _));

        Assert.Equal("0", key1.CanonicalValue);
        Assert.Equal("0", key2.CanonicalValue);
    }

    [Fact]
    public void RejectsInvalidTypes()
    {
        var canonicalizer = new KeyCanonicalizer();
        var nullElement = JsonDocument.Parse("null").RootElement;
        var objectElement = JsonDocument.Parse("{\"a\":1}").RootElement;
        var arrayElement = JsonDocument.Parse("[1]").RootElement;

        Assert.False(canonicalizer.TryCanonicalize(nullElement, out _, out var warning1));
        Assert.Equal(WarningCode.InvalidKey, warning1!.Code);
        Assert.Equal(WarningSeverity.Error, warning1.Severity);

        Assert.False(canonicalizer.TryCanonicalize(objectElement, out _, out var warning2));
        Assert.Equal(WarningCode.InvalidKey, warning2!.Code);
        Assert.Equal(WarningSeverity.Error, warning2.Severity);

        Assert.False(canonicalizer.TryCanonicalize(arrayElement, out _, out var warning3));
        Assert.Equal(WarningCode.InvalidKey, warning3!.Code);
        Assert.Equal(WarningSeverity.Error, warning3.Severity);
    }
}
