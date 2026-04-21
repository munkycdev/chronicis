using Chronicis.Api.Services;
using Xunit;

namespace Chronicis.Api.Tests;

public class WikiLinkTitleRewriterTests
{
    private static readonly Guid TargetId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid OtherId  = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static WikiLinkTitleRewriter Sut() => new();

    private static string Span(Guid id, string inner, string extraAttrs = "") =>
        $"<span data-type=\"wiki-link\" data-target-id=\"{id:D}\"{(extraAttrs.Length > 0 ? " " + extraAttrs : "")}>{inner}</span>";

    // ── basic rewrite ─────────────────────────────────────────────────────────

    [Fact]
    public void Rewrites_SimpleWikiLinkSpan_ToNewTitle()
    {
        var body = Span(TargetId, "OldTitle");
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.True(changed);
        Assert.Contains("NewTitle", result, StringComparison.Ordinal);
        Assert.DoesNotContain("OldTitle", result, StringComparison.Ordinal);
    }

    // ── attribute guards ──────────────────────────────────────────────────────

    [Fact]
    public void PreservesSpan_WhenDataDisplayPresent()
    {
        var body = Span(TargetId, "OldTitle", "data-display=\"custom\"");
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(body, result);
    }

    [Fact]
    public void PreservesSpan_WhenDataDisplayEmpty()
    {
        var body = Span(TargetId, "OldTitle", "data-display=\"\"");
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(body, result);
    }
    [Fact]
    public void SkipsMapChip_WhenDataMapIdPresent()
    {
        var body = Span(TargetId, "OldTitle", "data-map-id=\"some-map\"");
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(body, result);
    }

    [Fact]
    public void SkipsBrokenSpan_WhenDataBrokenTrue()
    {
        var body = Span(TargetId, "OldTitle", "data-broken=\"true\"");
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(body, result);
    }

    [Fact]
    public void SkipsExternalLinkSpan()
    {
        var body = $"<span data-type=\"external-link\" data-target-id=\"{TargetId:D}\">OldTitle</span>";
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(body, result);
    }

    [Fact]
    public void SkipsLegacyMarkdownLink()
    {
        var body = $"[[{TargetId:D}|OldTitle]]";
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(body, result);
    }

    [Fact]
    public void SkipsSpanWithNestedMarkup()
    {
        var body = $"<span data-type=\"wiki-link\" data-target-id=\"{TargetId:D}\"><em>OldTitle</em></span>";
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(body, result);
    }

    // ── multi-span scenarios ──────────────────────────────────────────────────

    [Fact]
    public void RewritesMultipleMatchingSpans_InSameBody()
    {
        var span = Span(TargetId, "OldTitle");
        var body = $"{span} and {span}";
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.True(changed);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(result, "NewTitle").Count);
    }
    [Fact]
    public void LeavesOtherTargetIds_Alone()
    {
        var match  = Span(TargetId, "OldTitle");
        var other  = Span(OtherId,  "OtherTitle");
        var body   = $"{match} {other}";
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.True(changed);
        Assert.Contains("NewTitle",   result, StringComparison.Ordinal);
        Assert.Contains("OtherTitle", result, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlEncodesNewTitle()
    {
        var body = Span(TargetId, "OldTitle");
        var (result, changed) = Sut().Rewrite(body, TargetId, "<Title & \"Stuff\">");

        Assert.True(changed);
        Assert.Contains("&lt;Title &amp; &quot;Stuff&quot;&gt;", result, StringComparison.Ordinal);
    }

    // ── no-match / empty guards ───────────────────────────────────────────────

    [Fact]
    public void ReturnsChangedFalse_WhenNoMatches()
    {
        var body = "<p>No wiki links here</p>";
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(body, result);
    }

    [Fact]
    public void ReturnsChangedFalse_WhenBodyIsNull()
    {
        var (result, changed) = Sut().Rewrite(null, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ReturnsChangedFalse_WhenBodyIsEmpty()
    {
        var (result, changed) = Sut().Rewrite(string.Empty, TargetId, "NewTitle");

        Assert.False(changed);
        Assert.Equal(string.Empty, result);
    }
    // ── attribute order / case independence ──────────────────────────────────

    [Fact]
    public void MatchesAttributeOrderIndependently()
    {
        // data-target-id appears BEFORE data-type
        var body = $"<span data-target-id=\"{TargetId:D}\" data-type=\"wiki-link\">OldTitle</span>";
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.True(changed);
        Assert.Contains("NewTitle", result, StringComparison.Ordinal);
    }

    [Fact]
    public void IsCaseInsensitiveForGuid()
    {
        // GUID in upper case in the HTML
        var upperGuid = TargetId.ToString("D").ToUpperInvariant();
        var body = $"<span data-type=\"wiki-link\" data-target-id=\"{upperGuid}\">OldTitle</span>";
        var (result, changed) = Sut().Rewrite(body, TargetId, "NewTitle");

        Assert.True(changed);
        Assert.Contains("NewTitle", result, StringComparison.Ordinal);
    }
}
