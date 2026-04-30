using Chronicis.Client.Services.Routing;
using Xunit;

namespace Chronicis.Client.Tests.Services.Routing;

public class AppUrlBuilderTests
{
    private readonly AppUrlBuilder _sut = new();

    // ─────────────────────────────────────────────────────────────────────
    // ForWorld
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForWorld_ReturnsSlashPrefixedSlug()
    {
        Assert.Equal("/middle-earth", _sut.ForWorld("middle-earth"));
    }

    [Fact]
    public void ForWorld_WithDashesAndDigits_Works()
    {
        Assert.Equal("/world-42", _sut.ForWorld("world-42"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ForWorld_EmptySlug_Throws(string? slug)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForWorld(slug!));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ForCampaign
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForCampaign_ReturnsTwoSegmentPath()
    {
        Assert.Equal("/middle-earth/war-of-the-ring", _sut.ForCampaign("middle-earth", "war-of-the-ring"));
    }

    [Theory]
    [InlineData("", "campaign")]
    [InlineData("world", "")]
    public void ForCampaign_EmptySlug_Throws(string worldSlug, string campaignSlug)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForCampaign(worldSlug, campaignSlug));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ForArc
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForArc_ReturnsThreeSegmentPath()
    {
        Assert.Equal("/me/wotr/fellowship", _sut.ForArc("me", "wotr", "fellowship"));
    }

    [Theory]
    [InlineData("", "c", "a")]
    [InlineData("w", "", "a")]
    [InlineData("w", "c", "")]
    public void ForArc_EmptySlug_Throws(string w, string c, string a)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForArc(w, c, a));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ForSession
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForSession_ReturnsFourSegmentPath()
    {
        Assert.Equal("/me/wotr/fellowship/session-1", _sut.ForSession("me", "wotr", "fellowship", "session-1"));
    }

    [Theory]
    [InlineData("", "c", "a", "s")]
    [InlineData("w", "", "a", "s")]
    [InlineData("w", "c", "", "s")]
    [InlineData("w", "c", "a", "")]
    public void ForSession_EmptySlug_Throws(string w, string c, string a, string s)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForSession(w, c, a, s));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ForSessionNote
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForSessionNote_ReturnsFiveSegmentPath()
    {
        Assert.Equal("/me/wotr/fellowship/session-1/my-note",
            _sut.ForSessionNote("me", "wotr", "fellowship", "session-1", "my-note"));
    }

    [Theory]
    [InlineData("", "c", "a", "s", "n")]
    [InlineData("w", "", "a", "s", "n")]
    [InlineData("w", "c", "", "s", "n")]
    [InlineData("w", "c", "a", "", "n")]
    [InlineData("w", "c", "a", "s", "")]
    public void ForSessionNote_EmptySlug_Throws(string w, string c, string a, string s, string n)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForSessionNote(w, c, a, s, n));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ForMapListing
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForMapListing_ReturnsMapsPath()
    {
        Assert.Equal("/middle-earth/maps", _sut.ForMapListing("middle-earth"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ForMapListing_EmptySlug_Throws(string? slug)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForMapListing(slug!));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ForMap
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForMap_ReturnsMapsSlugPath()
    {
        Assert.Equal("/middle-earth/maps/eriador", _sut.ForMap("middle-earth", "eriador"));
    }

    [Theory]
    [InlineData("", "map")]
    [InlineData("world", "")]
    public void ForMap_EmptySlug_Throws(string worldSlug, string mapSlug)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForMap(worldSlug, mapSlug));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ForWikiArticle
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForWikiArticle_SingleSegment_ReturnsWorldPlusArticle()
    {
        Assert.Equal("/middle-earth/wiki/rivendell", _sut.ForWikiArticle("middle-earth", ["rivendell"]));
    }

    [Fact]
    public void ForWikiArticle_MultipleSegments_BuildsFullPath()
    {
        Assert.Equal("/middle-earth/wiki/locations/rivendell",
            _sut.ForWikiArticle("middle-earth", ["locations", "rivendell"]));
    }

    [Fact]
    public void ForWikiArticle_SegmentsWithDashesAndDigits_Work()
    {
        Assert.Equal("/world-1/wiki/parent-node/child-42",
            _sut.ForWikiArticle("world-1", ["parent-node", "child-42"]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ForWikiArticle_EmptyWorldSlug_Throws(string? worldSlug)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForWikiArticle(worldSlug!, ["article"]));
    }

    [Fact]
    public void ForWikiArticle_NullSegments_Throws()
    {
        Assert.Throws<ArgumentException>(() => _sut.ForWikiArticle("world", null!));
    }

    [Fact]
    public void ForWikiArticle_EmptySegments_Throws()
    {
        Assert.Throws<ArgumentException>(() => _sut.ForWikiArticle("world", []));
    }

    // ─────────────────────────────────────────────────────────────────────
    // ForTutorial
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ForTutorial_ReturnsTutorialsPrefixedPath()
    {
        Assert.Equal("/tutorials/getting-started", _sut.ForTutorial("getting-started"));
    }

    [Fact]
    public void ForTutorial_WithDashesAndDigits_Works()
    {
        Assert.Equal("/tutorials/step-42", _sut.ForTutorial("step-42"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ForTutorial_EmptySlug_Throws(string? slug)
    {
        Assert.Throws<ArgumentException>(() => _sut.ForTutorial(slug!));
    }
}
