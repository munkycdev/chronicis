namespace Chronicis.Shared.Tests.DTOs;

/// <summary>
/// Tests for DTOs with computed properties and business logic.
/// </summary>
public class ComputedPropertyDtosTests
{
    // ────────────────────────────────────────────────────────────────
    //  ActiveContextDto - HasActiveContext computed property
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ActiveContextDto_HasActiveContext_ReturnsFalse_WhenBothIdsAreNull()
    {
        var dto = new ActiveContextDto
        {
            CampaignId = null,
            ArcId = null
        };

        Assert.False(dto.HasActiveContext);
    }

    [Fact]
    public void ActiveContextDto_HasActiveContext_ReturnsFalse_WhenOnlyCampaignIdIsSet()
    {
        var dto = new ActiveContextDto
        {
            CampaignId = Guid.NewGuid(),
            ArcId = null
        };

        Assert.False(dto.HasActiveContext);
    }

    [Fact]
    public void ActiveContextDto_HasActiveContext_ReturnsFalse_WhenOnlyArcIdIsSet()
    {
        var dto = new ActiveContextDto
        {
            CampaignId = null,
            ArcId = Guid.NewGuid()
        };

        Assert.False(dto.HasActiveContext);
    }

    [Fact]
    public void ActiveContextDto_HasActiveContext_ReturnsTrue_WhenBothIdsAreSet()
    {
        var dto = new ActiveContextDto
        {
            CampaignId = Guid.NewGuid(),
            ArcId = Guid.NewGuid()
        };

        Assert.True(dto.HasActiveContext);
    }

    // ────────────────────────────────────────────────────────────────
    //  EntitySummaryDto - HasSummary computed property
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void EntitySummaryDto_HasSummary_ReturnsFalse_WhenSummaryIsNull()
    {
        var dto = new EntitySummaryDto
        {
            Summary = null
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void EntitySummaryDto_HasSummary_ReturnsFalse_WhenSummaryIsEmpty()
    {
        var dto = new EntitySummaryDto
        {
            Summary = string.Empty
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void EntitySummaryDto_HasSummary_ReturnsFalse_WhenSummaryIsWhitespace()
    {
        var dto = new EntitySummaryDto
        {
            Summary = "   "
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void EntitySummaryDto_HasSummary_ReturnsTrue_WhenSummaryHasContent()
    {
        var dto = new EntitySummaryDto
        {
            Summary = "This is a summary"
        };

        Assert.True(dto.HasSummary);
    }

    // ────────────────────────────────────────────────────────────────
    //  ArticleSummaryDto - HasSummary computed property
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ArticleSummaryDto_HasSummary_ReturnsFalse_WhenSummaryIsNull()
    {
        var dto = new ArticleSummaryDto
        {
            Summary = null
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void ArticleSummaryDto_HasSummary_ReturnsFalse_WhenSummaryIsEmpty()
    {
        var dto = new ArticleSummaryDto
        {
            Summary = string.Empty
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void ArticleSummaryDto_HasSummary_ReturnsFalse_WhenSummaryIsWhitespace()
    {
        var dto = new ArticleSummaryDto
        {
            Summary = "   "
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void ArticleSummaryDto_HasSummary_ReturnsTrue_WhenSummaryHasContent()
    {
        var dto = new ArticleSummaryDto
        {
            Summary = "Generated AI summary"
        };

        Assert.True(dto.HasSummary);
    }

    // ────────────────────────────────────────────────────────────────
    //  SummaryPreviewDto - HasSummary computed property
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void SummaryPreviewDto_HasSummary_ReturnsFalse_WhenSummaryIsNull()
    {
        var dto = new SummaryPreviewDto
        {
            Summary = null
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void SummaryPreviewDto_HasSummary_ReturnsFalse_WhenSummaryIsEmpty()
    {
        var dto = new SummaryPreviewDto
        {
            Summary = string.Empty
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void SummaryPreviewDto_HasSummary_ReturnsFalse_WhenSummaryIsWhitespace()
    {
        var dto = new SummaryPreviewDto
        {
            Summary = "   "
        };

        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void SummaryPreviewDto_HasSummary_ReturnsTrue_WhenSummaryHasContent()
    {
        var dto = new SummaryPreviewDto
        {
            Summary = "Preview text"
        };

        Assert.True(dto.HasSummary);
    }

    // ────────────────────────────────────────────────────────────────
    //  Integration tests verifying default values
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ActiveContextDto_DefaultState_HasNoActiveContext()
    {
        var dto = new ActiveContextDto();
        
        Assert.Null(dto.WorldId);
        Assert.Null(dto.CampaignId);
        Assert.Null(dto.ArcId);
        Assert.False(dto.HasActiveContext);
    }

    [Fact]
    public void EntitySummaryDto_DefaultState_HasNoSummary()
    {
        var dto = new EntitySummaryDto();
        
        Assert.Null(dto.Summary);
        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void ArticleSummaryDto_DefaultState_HasNoSummary()
    {
        var dto = new ArticleSummaryDto();
        
        Assert.Null(dto.Summary);
        Assert.False(dto.HasSummary);
    }

    [Fact]
    public void SummaryPreviewDto_DefaultState_HasNoSummary()
    {
        var dto = new SummaryPreviewDto();
        
        Assert.Null(dto.Summary);
        Assert.False(dto.HasSummary);
        Assert.Equal(string.Empty, dto.Title);
    }
}
