using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Controllers;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services.Routing;
using Chronicis.Shared.Models;
using Chronicis.Shared.Routing;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class PathsControllerTests
{
    private record Sut(PathsController Controller, ISlugPathResolver Resolver, ICurrentUserService CurrentUser);

    private static Sut CreateSut(bool isAuthenticated = false, Guid? userId = null)
    {
        var resolver = Substitute.For<ISlugPathResolver>();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(isAuthenticated);

        if (isAuthenticated && userId.HasValue)
        {
            var user = new User { Id = userId.Value, DisplayName = "Tester", Email = "t@t.com", Auth0UserId = "auth0|t" };
            currentUser.GetCurrentUserAsync().Returns(user);
        }

        return new Sut(new PathsController(resolver, currentUser), resolver, currentUser);
    }

    private static SlugPathResolution MakeResolution(ResolvedEntityKind kind, Guid? worldId = null) =>
        new(kind, worldId ?? Guid.NewGuid(), null, null, null, null, null, Array.Empty<SlugPathBreadcrumb>());

    // ─────────────────────────────────────────────────────────────────────
    // Empty / invalid path
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("   ")]
    public async Task Resolve_EmptyPath_ReturnsNotFound(string path)
    {
        var sut = CreateSut();
        var result = await sut.Controller.Resolve(path, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Theory]
    [InlineData("bad segment!")]
    [InlineData("my-world/bad segment!")]
    [InlineData("my-world/has_underscore")]
    public async Task Resolve_InvalidSegment_ReturnsBadRequest(string path)
    {
        var sut = CreateSut();
        var result = await sut.Controller.Resolve(path, CancellationToken.None);
        Assert.IsType<BadRequestResult>(result.Result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Keyword segments are exempt from slug validation
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("my-world/wiki")]
    [InlineData("my-world/maps")]
    [InlineData("my-world/WIKI")]
    public async Task Resolve_KeywordSegments_AreValidAndPassThroughToResolver(string path)
    {
        var sut = CreateSut();
        sut.Resolver.ResolveAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((SlugPathResolution?)null);

        var result = await sut.Controller.Resolve(path, CancellationToken.None);

        // Not 400 — segment validation passed (may be 404 since resolver returns null)
        Assert.IsType<NotFoundResult>(result.Result);
        await sut.Resolver.Received(1).ResolveAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Resolver returns null → 404
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_ResolverReturnsNull_ReturnsNotFound()
    {
        var sut = CreateSut();
        sut.Resolver.ResolveAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((SlugPathResolution?)null);

        var result = await sut.Controller.Resolve("my-world", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Resolver returns a result → 200 Ok
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ResolvedEntityKind.World)]
    [InlineData(ResolvedEntityKind.Campaign)]
    [InlineData(ResolvedEntityKind.Arc)]
    [InlineData(ResolvedEntityKind.Session)]
    [InlineData(ResolvedEntityKind.Map)]
    [InlineData(ResolvedEntityKind.WikiArticle)]
    public async Task Resolve_ResolverReturnsResolution_ReturnsOkWithBody(ResolvedEntityKind kind)
    {
        var sut = CreateSut();
        var expected = MakeResolution(kind);
        sut.Resolver.ResolveAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await sut.Controller.Resolve("my-world", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resolution = Assert.IsType<SlugPathResolution>(ok.Value);
        Assert.Equal(kind, resolution.Kind);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Anonymous access: currentUserId passed as null
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_AnonymousUser_PassesNullUserIdToResolver()
    {
        var sut = CreateSut(isAuthenticated: false);
        sut.Resolver.ResolveAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((SlugPathResolution?)null);

        await sut.Controller.Resolve("my-world", CancellationToken.None);

        await sut.Resolver.Received(1).ResolveAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Is<Guid?>(id => id == null),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Authenticated access: currentUserId passed through
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_AuthenticatedUser_PassesUserIdToResolver()
    {
        var userId = Guid.NewGuid();
        var sut = CreateSut(isAuthenticated: true, userId: userId);
        sut.Resolver.ResolveAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((SlugPathResolution?)null);

        await sut.Controller.Resolve("my-world", CancellationToken.None);

        await sut.Resolver.Received(1).ResolveAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Is<Guid?>(id => id == userId),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Authenticated but GetCurrentUserAsync returns null → null userId
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_AuthenticatedButUserNull_PassesNullUserIdToResolver()
    {
        var sut = CreateSut(isAuthenticated: true);
        sut.CurrentUser.GetCurrentUserAsync().Returns((User?)null);
        sut.Resolver.ResolveAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((SlugPathResolution?)null);

        await sut.Controller.Resolve("my-world", CancellationToken.None);

        await sut.Resolver.Received(1).ResolveAsync(
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Is<Guid?>(id => id == null),
            Arg.Any<CancellationToken>());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Multi-segment paths
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_MultiSegmentPath_PassesAllSegmentsNormalized()
    {
        var sut = CreateSut();
        IReadOnlyList<string>? capturedSegments = null;
        sut.Resolver.ResolveAsync(
            Arg.Do<IReadOnlyList<string>>(s => capturedSegments = s),
            Arg.Any<Guid?>(),
            Arg.Any<CancellationToken>())
            .Returns((SlugPathResolution?)null);

        await sut.Controller.Resolve("my-world/campaign-1/arc-2", CancellationToken.None);

        Assert.NotNull(capturedSegments);
        Assert.Equal(new[] { "my-world", "campaign-1", "arc-2" }, capturedSegments);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 5-segment session-note path resolution
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Resolve_FiveSegmentSessionNotePath_ReturnsSessionNoteKindWithIds()
    {
        var worldId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var resolution = new SlugPathResolution(
            Kind: ResolvedEntityKind.SessionNote,
            WorldId: worldId,
            CampaignId: Guid.NewGuid(),
            ArcId: Guid.NewGuid(),
            SessionId: sessionId,
            MapId: null,
            ArticleId: articleId,
            Breadcrumbs: Array.Empty<SlugPathBreadcrumb>());

        var sut = CreateSut(isAuthenticated: true, userId: Guid.NewGuid());
        sut.Resolver.ResolveAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(resolution);

        var result = await sut.Controller.Resolve(
            "my-world/my-campaign/my-arc/session-1/munkys-notes",
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var resolved = Assert.IsType<SlugPathResolution>(ok.Value);
        Assert.Equal(ResolvedEntityKind.SessionNote, resolved.Kind);
        Assert.Equal(sessionId, resolved.SessionId);
        Assert.Equal(articleId, resolved.ArticleId);
    }
}
