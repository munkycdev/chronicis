using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services;
using Xunit;

namespace Chronicis.Api.Tests;


[ExcludeFromCodeCoverage]
public class LinkParserTests
{
    private readonly LinkParser _parser;

    public LinkParserTests()
    {
        _parser = new LinkParser();
    }

    // ────────────────────────────────────────────────────────────────
    //  ParseLinks - Legacy Format [[guid|text]]
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseLinks_LegacyFormat_SimpleLink_ParsesCorrectly()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var body = $"Check out [[{guid}]] for more info.";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Single(links);
        Assert.Equal(guid, links[0].TargetArticleId);
        Assert.Null(links[0].DisplayText);
    }

    [Fact]
    public void ParseLinks_LegacyFormat_WithDisplayText_ParsesCorrectly()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var body = $"See [[{guid}|The Magic System]] for details.";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Single(links);
        Assert.Equal(guid, links[0].TargetArticleId);
        Assert.Equal("The Magic System", links[0].DisplayText);
    }

    [Fact]
    public void ParseLinks_LegacyFormat_MultipleLinks_ParsesAll()
    {
        var guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var body = $"See [[{guid1}|First]] and [[{guid2}|Second]].";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Equal(2, links.Count);
        Assert.Equal(guid1, links[0].TargetArticleId);
        Assert.Equal(guid2, links[1].TargetArticleId);
    }

    [Fact]
    public void ParseLinks_LegacyFormat_DuplicateGuids_ReturnsUnique()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var body = $"See [[{guid}]] and also [[{guid}|Same Article]].";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Single(links); // Only one unique link
        Assert.Equal(guid, links[0].TargetArticleId);
    }

    [Fact]
    public void ParseLinks_LegacyFormat_InvalidGuid_Ignored()
    {
        var body = "See [[not-a-guid]] for info.";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Empty(links);
    }

    // ────────────────────────────────────────────────────────────────
    //  ParseLinks - HTML Format (TipTap)
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseLinks_HtmlFormat_SimpleLink_ParsesCorrectly()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var body = $"<p>Check <span data-target-id=\"{guid}\" class=\"wiki-link\">Magic System</span> out.</p>";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Single(links);
        Assert.Equal(guid, links[0].TargetArticleId);
        Assert.Equal("Magic System", links[0].DisplayText);
    }

    [Fact]
    public void ParseLinks_HtmlFormat_MultipleLinks_ParsesAll()
    {
        var guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var body = $"<p>See <span data-target-id=\"{guid1}\">First</span> and <span data-target-id=\"{guid2}\">Second</span>.</p>";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Equal(2, links.Count);
        Assert.Equal(guid1, links[0].TargetArticleId);
        Assert.Equal(guid2, links[1].TargetArticleId);
    }

    [Fact]
    public void ParseLinks_HtmlFormat_WithExtraAttributes_ParsesCorrectly()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var body = $"<span class=\"wiki-link\" data-target-id=\"{guid}\" data-other=\"value\">Display Text</span>";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Single(links);
        Assert.Equal(guid, links[0].TargetArticleId);
        Assert.Equal("Display Text", links[0].DisplayText);
    }

    // ────────────────────────────────────────────────────────────────
    //  ParseLinks - Mixed Formats
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseLinks_MixedFormats_ParsesBoth()
    {
        var guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var body = $"Legacy [[{guid1}|Legacy Link]] and HTML <span data-target-id=\"{guid2}\">HTML Link</span>.";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Equal(2, links.Count);
    }

    [Fact]
    public void ParseLinks_MixedFormats_DuplicateGuids_ReturnsUnique()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var body = $"HTML <span data-target-id=\"{guid}\">First</span> and legacy [[{guid}|Second]].";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Single(links); // Only one unique GUID
        Assert.Equal(guid, links[0].TargetArticleId);
    }

    // ────────────────────────────────────────────────────────────────
    //  ParseLinks - Edge Cases
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ParseLinks_NullBody_ReturnsEmpty()
    {
        var links = _parser.ParseLinks(null).ToList();

        Assert.Empty(links);
    }

    [Fact]
    public void ParseLinks_EmptyBody_ReturnsEmpty()
    {
        var links = _parser.ParseLinks("").ToList();

        Assert.Empty(links);
    }

    [Fact]
    public void ParseLinks_NoLinks_ReturnsEmpty()
    {
        var body = "Just plain text with no links.";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Empty(links);
    }

    [Fact]
    public void ParseLinks_WhitespaceInDisplayText_Trimmed()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var body = $"<span data-target-id=\"{guid}\">  Trimmed Text  </span>";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Single(links);
        Assert.Equal("Trimmed Text", links[0].DisplayText);
    }

    [Fact]
    public void ParseLinks_CaseInsensitiveGuid_ParsesCorrectly()
    {
        var guid = Guid.Parse("ABCDEF12-ABCD-ABCD-ABCD-ABCDEFABCDEF");
        var bodyLower = $"[[{guid.ToString().ToLower()}]]";
        var bodyUpper = $"[[{guid.ToString().ToUpper()}]]";

        var linksLower = _parser.ParseLinks(bodyLower).ToList();
        var linksUpper = _parser.ParseLinks(bodyUpper).ToList();

        Assert.Single(linksLower);
        Assert.Single(linksUpper);
        Assert.Equal(guid, linksLower[0].TargetArticleId);
        Assert.Equal(guid, linksUpper[0].TargetArticleId);
    }

    [Fact]
    public void ParseLinks_PositionTracking_ReturnsCorrectIndex()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var body = $"Text before [[{guid}]] and after.";

        var links = _parser.ParseLinks(body).ToList();

        Assert.Single(links);
        Assert.Equal(12, links[0].Position); // Position of "[["
    }
}
