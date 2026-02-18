using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ICharacterApiServiceTests
{
    [Fact]
    public void CharacterClaimStatusDto_SetsProperties()
    {
        var id = Guid.NewGuid();
        var dto = new CharacterClaimStatusDto
        {
            CharacterId = id,
            IsClaimed = true,
            IsClaimedByMe = false,
            ClaimedByName = "Alice"
        };

        Assert.Equal(id, dto.CharacterId);
        Assert.True(dto.IsClaimed);
        Assert.False(dto.IsClaimedByMe);
        Assert.Equal("Alice", dto.ClaimedByName);
    }
}

