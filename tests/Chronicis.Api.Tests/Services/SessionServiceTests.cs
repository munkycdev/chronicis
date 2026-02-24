using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Data;
using Chronicis.Api.Models;
using Chronicis.Api.Services;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class SessionServiceTests : IDisposable
{
    private readonly ChronicisDbContext _context;
    private readonly SessionService _service;

    public SessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ChronicisDbContext(options);
        _service = new SessionService(_context, NullLogger<SessionService>.Instance);

        SeedTestData();
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task UpdateSessionNotesAsync_NonGm_ReturnsForbidden()
    {
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ArcId = TestHelpers.FixedIds.Arc1,
            Name = "Session 1",
            PublicNotes = "Before public",
            PrivateNotes = "Before private",
            CreatedBy = TestHelpers.FixedIds.User1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var dto = new SessionUpdateDto
        {
            PublicNotes = "After public",
            PrivateNotes = "After private"
        };

        var result = await _service.UpdateSessionNotesAsync(session.Id, dto, TestHelpers.FixedIds.User2);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);

        var unchanged = await _context.Sessions.FindAsync(session.Id);
        Assert.NotNull(unchanged);
        Assert.Equal("Before public", unchanged!.PublicNotes);
        Assert.Equal("Before private", unchanged.PrivateNotes);
    }

    [Fact]
    public async Task CreateSessionAsync_CreatesExactlyOneDefaultPublicSessionNote()
    {
        var dto = new SessionCreateDto
        {
            Name = "The Dark Forest",
            SessionDate = new DateTime(2026, 2, 24, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _service.CreateSessionAsync(
            TestHelpers.FixedIds.Arc1,
            dto,
            TestHelpers.FixedIds.User1,
            "GM User");

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);

        var savedSession = await _context.Sessions.FindAsync(result.Value!.Id);
        Assert.NotNull(savedSession);

        var notes = await _context.Articles
            .Where(a => a.SessionId == result.Value.Id)
            .ToListAsync();

        Assert.Single(notes);

        var note = notes[0];
        Assert.Equal(ArticleType.SessionNote, note.Type);
        Assert.Equal(ArticleVisibility.Public, note.Visibility);
        Assert.Equal(result.Value.Id, note.SessionId);
        Assert.Equal("GM User's Notes", note.Title);
    }

    private void SeedTestData()
    {
        var (world, gm, player) = TestHelpers.SeedBasicWorld(_context);

        var campaign = TestHelpers.CreateCampaign(
            id: TestHelpers.FixedIds.Campaign1,
            worldId: world.Id,
            name: "Test Campaign");
        campaign.OwnerId = gm.Id;
        campaign.Owner = gm;
        _context.Campaigns.Add(campaign);

        var arc = TestHelpers.CreateArc(
            id: TestHelpers.FixedIds.Arc1,
            campaignId: campaign.Id,
            name: "Act 1");
        arc.CreatedBy = gm.Id;
        arc.Creator = gm;
        _context.Arcs.Add(arc);

        _context.SaveChanges();
    }
}
