using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Shared.Tests.Models;

/// <summary>
/// Tests for the Session domain model.
/// </summary>
[ExcludeFromCodeCoverage]
public class SessionTests
{
    [Fact]
    public void Session_HasParameterlessConstructor()
    {
        var session = new Session();
        Assert.NotNull(session);
    }

    [Fact]
    public void Session_DefaultValues_AreCorrect()
    {
        var session = new Session();

        Assert.Equal(string.Empty, session.Name);
        Assert.Null(session.SessionDate);
        Assert.Null(session.PublicNotes);
        Assert.Null(session.PrivateNotes);
        Assert.Null(session.AiSummary);
        Assert.Null(session.AiSummaryGeneratedAt);
        Assert.Null(session.AiSummaryGeneratedByUserId);
        Assert.NotNull(session.SessionNotes);
        Assert.NotNull(session.QuestUpdates);
    }

    [Fact]
    public void Session_CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var session = new Session();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(session.CreatedAt, before, after);
    }

    [Fact]
    public void Session_ModifiedAt_DefaultsToNull()
    {
        var session = new Session();
        Assert.Null(session.ModifiedAt);
    }

    [Fact]
    public void Session_Properties_CanBeSetAndRetrieved()
    {
        var id = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var generatedBy = Guid.NewGuid();
        var sessionDate = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        var session = new Session
        {
            Id = id,
            ArcId = arcId,
            Name = "Session 1 — The Dark Forest",
            SessionDate = sessionDate,
            PublicNotes = "<p>Public GM notes</p>",
            PrivateNotes = "<p>Secret GM notes</p>",
            AiSummary = "AI-generated summary",
            AiSummaryGeneratedAt = now,
            AiSummaryGeneratedByUserId = generatedBy,
            CreatedAt = now,
            ModifiedAt = now,
            CreatedBy = createdBy
        };

        Assert.Equal(id, session.Id);
        Assert.Equal(arcId, session.ArcId);
        Assert.Equal("Session 1 — The Dark Forest", session.Name);
        Assert.Equal(sessionDate, session.SessionDate);
        Assert.Equal("<p>Public GM notes</p>", session.PublicNotes);
        Assert.Equal("<p>Secret GM notes</p>", session.PrivateNotes);
        Assert.Equal("AI-generated summary", session.AiSummary);
        Assert.Equal(now, session.AiSummaryGeneratedAt);
        Assert.Equal(generatedBy, session.AiSummaryGeneratedByUserId);
        Assert.Equal(now, session.CreatedAt);
        Assert.Equal(now, session.ModifiedAt);
        Assert.Equal(createdBy, session.CreatedBy);
    }

    [Fact]
    public void Session_NavigationProperties_InitializeAsEmpty()
    {
        var session = new Session();

        Assert.Empty(session.SessionNotes);
        Assert.Empty(session.QuestUpdates);
    }

    [Fact]
    public void Session_NullableNavigationProperties_AreNull()
    {
        var session = new Session();

        Assert.Null(session.AiSummaryGeneratedBy);
    }

    [Fact]
    public void Session_PrivateNotes_IsDistinctFromPublicNotes()
    {
        var session = new Session
        {
            PublicNotes = "Public content",
            PrivateNotes = "Private content"
        };

        Assert.NotEqual(session.PublicNotes, session.PrivateNotes);
    }
}
