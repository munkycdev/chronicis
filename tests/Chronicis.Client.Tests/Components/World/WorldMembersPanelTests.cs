using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Client.Components.World;
using Chronicis.Shared.Enums;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components.World;

[ExcludeFromCodeCoverage]
public class WorldMembersPanelTests
{
    [Theory]
    [InlineData(WorldRole.GM, Color.Warning)]
    [InlineData(WorldRole.Player, Color.Primary)]
    [InlineData(WorldRole.Observer, Color.Default)]
    [InlineData((WorldRole)999, Color.Default)]
    public void GetRoleColor_ReturnsExpectedColor(WorldRole role, Color expected)
    {
        var method = typeof(WorldMembersPanel)
            .GetMethod("GetRoleColor", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var result = (Color?)method!.Invoke(null, [role]);
        Assert.Equal(expected, result);
    }
}
