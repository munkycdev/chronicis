using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.Enums;
using Xunit;

namespace Chronicis.Api.Tests;

public class CharacterClaimServiceTests
{
    [Fact]
    public async Task GetClaimedCharactersAsync_ReturnsWorldAndFallbackNames()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId, name: "Known World");
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        var knownWorldCharacter = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: "Known",
            type: ArticleType.Character);
        knownWorldCharacter.PlayerId = userId;

        var unknownWorldCharacter = TestHelpers.CreateArticle(
            worldId: Guid.NewGuid(),
            createdBy: userId,
            title: "Unknown",
            type: ArticleType.Character);
        unknownWorldCharacter.PlayerId = userId;

        var nonCharacter = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            title: "Ignore",
            type: ArticleType.WikiArticle);
        nonCharacter.PlayerId = userId;

        db.Articles.AddRange(knownWorldCharacter, unknownWorldCharacter, nonCharacter);
        await db.SaveChangesAsync();

        var sut = new CharacterClaimService(db);

        var claimed = await sut.GetClaimedCharactersAsync(userId);

        Assert.Equal(2, claimed.Count);
        Assert.Contains(claimed, c => c.Title == "Known" && c.WorldName == "Known World");
        Assert.Contains(claimed, c => c.Title == "Unknown" && c.WorldName == "Unknown World");
    }

    [Fact]
    public async Task GetClaimStatusAsync_CoversMissingAndFoundBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var user = TestHelpers.CreateUser(displayName: "Player One");
        db.Users.Add(user);

        var claimed = TestHelpers.CreateArticle(
            type: ArticleType.Character,
            title: "Claimed Character");
        claimed.PlayerId = user.Id;

        var unclaimed = TestHelpers.CreateArticle(
            type: ArticleType.Character,
            title: "Unclaimed Character");
        unclaimed.PlayerId = null;

        db.Articles.AddRange(claimed, unclaimed);
        await db.SaveChangesAsync();

        var sut = new CharacterClaimService(db);

        var missing = await sut.GetClaimStatusAsync(Guid.NewGuid());
        Assert.False(missing.Found);

        var claimedStatus = await sut.GetClaimStatusAsync(claimed.Id);
        Assert.True(claimedStatus.Found);
        Assert.Equal(user.Id, claimedStatus.PlayerId);
        Assert.Equal("Player One", claimedStatus.PlayerName);

        var unclaimedStatus = await sut.GetClaimStatusAsync(unclaimed.Id);
        Assert.True(unclaimedStatus.Found);
        Assert.Null(unclaimedStatus.PlayerId);
        Assert.Null(unclaimedStatus.PlayerName);
    }

    [Fact]
    public async Task ClaimCharacterAsync_ReturnsNotFound_WhenNoAccess()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var owner = Guid.NewGuid();
        var caller = Guid.NewGuid();

        var world = TestHelpers.CreateWorld(ownerId: owner);
        db.Worlds.Add(world);

        var character = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: owner,
            type: ArticleType.Character);
        db.Articles.Add(character);
        await db.SaveChangesAsync();

        var sut = new CharacterClaimService(db);
        var result = await sut.ClaimCharacterAsync(character.Id, caller);

        Assert.Equal(ServiceStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ClaimCharacterAsync_ReturnsConflict_WhenClaimedByAnotherUser()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: otherUserId);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        var character = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: otherUserId,
            type: ArticleType.Character);
        character.PlayerId = otherUserId;
        db.Articles.Add(character);
        await db.SaveChangesAsync();

        var sut = new CharacterClaimService(db);
        var result = await sut.ClaimCharacterAsync(character.Id, userId);

        Assert.Equal(ServiceStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task ClaimCharacterAsync_ReturnsSuccess_WhenAvailable()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var owner = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: owner);
        db.Worlds.Add(world);
        db.WorldMembers.Add(TestHelpers.CreateWorldMember(worldId: world.Id, userId: userId));

        var character = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: owner,
            type: ArticleType.Character);
        db.Articles.Add(character);
        await db.SaveChangesAsync();

        var sut = new CharacterClaimService(db);
        var result = await sut.ClaimCharacterAsync(character.Id, userId);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.Equal(userId, db.Articles.Single(a => a.Id == character.Id).PlayerId);
    }

    [Fact]
    public async Task UnclaimCharacterAsync_CoversNotFoundAndSuccessBranches()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var userId = Guid.NewGuid();
        var world = TestHelpers.CreateWorld(ownerId: userId);
        db.Worlds.Add(world);

        var character = TestHelpers.CreateArticle(
            worldId: world.Id,
            createdBy: userId,
            type: ArticleType.Character);
        character.PlayerId = userId;
        db.Articles.Add(character);
        await db.SaveChangesAsync();

        var sut = new CharacterClaimService(db);

        var missing = await sut.UnclaimCharacterAsync(Guid.NewGuid(), userId);
        Assert.Equal(ServiceStatus.NotFound, missing.Status);

        var success = await sut.UnclaimCharacterAsync(character.Id, userId);
        Assert.Equal(ServiceStatus.Success, success.Status);
        Assert.Null(db.Articles.Single(a => a.Id == character.Id).PlayerId);
    }
}
