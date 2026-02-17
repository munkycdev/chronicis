using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class UserServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly UserService _service;
    private readonly IWorldService _worldService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _worldService = Substitute.For<IWorldService>();
        _service = new UserService(_context, _worldService, NullLogger<UserService>.Instance);
    }

    private bool _disposed = false;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _context.Dispose();
        }

        _disposed = true;
    }

    // ────────────────────────────────────────────────────────────────
    //  GetOrCreateUserAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateUserAsync_NewUser_CreatesUser()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|12345",
            "test@example.com",
            "Test User",
            "https://example.com/avatar.jpg");

        Assert.NotNull(user);
        Assert.Equal("auth0|12345", user.Auth0UserId);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("Test User", user.DisplayName);
        Assert.Equal("https://example.com/avatar.jpg", user.AvatarUrl);
        Assert.False(user.HasCompletedOnboarding);

        // Verify saved to database
        var saved = await _context.Users.FirstOrDefaultAsync(u => u.Auth0UserId == "auth0|12345");
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_ExistingUser_ReturnsExisting()
    {
        // Create user first
        var existing = await _service.GetOrCreateUserAsync(
            "auth0|existing",
            "existing@example.com",
            "Existing User",
            null);

        var existingId = existing.Id;

        // Try to create again
        var retrieved = await _service.GetOrCreateUserAsync(
            "auth0|existing",
            "existing@example.com",
            "Existing User",
            null);

        Assert.Equal(existingId, retrieved.Id);
        Assert.Single(await _context.Users.Where(u => u.Auth0UserId == "auth0|existing").ToListAsync());
    }

    [Fact]
    public async Task GetOrCreateUserAsync_ExistingUser_UpdatesChangedInfo()
    {
        // Create user
        await _service.GetOrCreateUserAsync(
            "auth0|update",
            "old@example.com",
            "Old Name",
            null);

        // Update with new info
        var updated = await _service.GetOrCreateUserAsync(
            "auth0|update",
            "new@example.com",
            "New Name",
            "https://example.com/new.jpg");

        Assert.Equal("new@example.com", updated.Email);
        Assert.Equal("New Name", updated.DisplayName);
        Assert.Equal("https://example.com/new.jpg", updated.AvatarUrl);
    }

    [Fact]
    public async Task GetOrCreateUserAsync_UpdatesLastLoginTime()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|login",
            "login@example.com",
            "Login User",
            null);

        var firstLogin = user.LastLoginAt;
        await Task.Delay(10); // Small delay

        // Login again
        var loginAgain = await _service.GetOrCreateUserAsync(
            "auth0|login",
            "login@example.com",
            "Login User",
            null);

        Assert.True(loginAgain.LastLoginAt > firstLogin);
    }

    // ────────────────────────────────────────────────────────────────
    //  GetUserByIdAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        var created = await _service.GetOrCreateUserAsync(
            "auth0|find",
            "find@example.com",
            "Find Me",
            null);

        var found = await _service.GetUserByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistent_ReturnsNull()
    {
        var found = await _service.GetUserByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateLastLoginAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLastLoginAsync_ExistingUser_UpdatesTimestamp()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|timestamp",
            "timestamp@example.com",
            "Timestamp User",
            null);

        var original = user.LastLoginAt;
        await Task.Delay(10);

        await _service.UpdateLastLoginAsync(user.Id);

        var updated = await _context.Users.FindAsync(user.Id);
        Assert.True(updated!.LastLoginAt > original);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_NonExistent_DoesNotThrow()
    {
        // Should not throw exception
        await _service.UpdateLastLoginAsync(Guid.NewGuid());
    }

    // ────────────────────────────────────────────────────────────────
    //  GetUserProfileAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProfileAsync_ExistingUser_ReturnsProfile()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|profile",
            "profile@example.com",
            "Profile User",
            "https://example.com/profile.jpg");

        var profile = await _service.GetUserProfileAsync(user.Id);

        Assert.NotNull(profile);
        Assert.Equal(user.Id, profile!.Id);
        Assert.Equal("profile@example.com", profile.Email);
        Assert.Equal("Profile User", profile.DisplayName);
        Assert.Equal("https://example.com/profile.jpg", profile.AvatarUrl);
        Assert.False(profile.HasCompletedOnboarding);
    }

    [Fact]
    public async Task GetUserProfileAsync_NonExistent_ReturnsNull()
    {
        var profile = await _service.GetUserProfileAsync(Guid.NewGuid());

        Assert.Null(profile);
    }

    // ────────────────────────────────────────────────────────────────
    //  CompleteOnboardingAsync
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteOnboardingAsync_ExistingUser_SetsFlag()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|onboarding",
            "onboarding@example.com",
            "Onboarding User",
            null);

        Assert.False(user.HasCompletedOnboarding);

        var result = await _service.CompleteOnboardingAsync(user.Id);

        Assert.True(result);

        var updated = await _context.Users.FindAsync(user.Id);
        Assert.True(updated!.HasCompletedOnboarding);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_AlreadyCompleted_Succeeds()
    {
        var user = await _service.GetOrCreateUserAsync(
            "auth0|completed",
            "completed@example.com",
            "Completed User",
            null);

        await _service.CompleteOnboardingAsync(user.Id);
        var result = await _service.CompleteOnboardingAsync(user.Id); // Complete again

        Assert.True(result);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_NonExistent_ReturnsFalse()
    {
        var result = await _service.CompleteOnboardingAsync(Guid.NewGuid());

        Assert.False(result);
    }
}
