namespace Chronicis.Shared.Tests;

/// <summary>
/// Tests for the WorldRole enum to ensure all expected values exist
/// and can be properly parsed and converted.
/// </summary>
public class WorldRoleTests
{
    [Fact]
    public void WorldRole_HasGM()
    {
        var value = WorldRole.GM;
        Assert.Equal(0, (int)value);
    }

    [Fact]
    public void WorldRole_HasPlayer()
    {
        var value = WorldRole.Player;
        Assert.Equal(1, (int)value);
    }

    [Fact]
    public void WorldRole_HasObserver()
    {
        var value = WorldRole.Observer;
        Assert.Equal(2, (int)value);
    }

    [Fact]
    public void WorldRole_GetValues_ReturnsAllExpectedValues()
    {
        var values = Enum.GetValues<WorldRole>();
        
        Assert.Equal(3, values.Length);
        Assert.Contains(WorldRole.GM, values);
        Assert.Contains(WorldRole.Player, values);
        Assert.Contains(WorldRole.Observer, values);
    }

    [Fact]
    public void WorldRole_GetNames_ReturnsCorrectNames()
    {
        var names = Enum.GetNames<WorldRole>();
        
        Assert.Equal(3, names.Length);
        Assert.Contains("GM", names);
        Assert.Contains("Player", names);
        Assert.Contains("Observer", names);
    }

    [Theory]
    [InlineData("GM", WorldRole.GM)]
    [InlineData("Player", WorldRole.Player)]
    [InlineData("Observer", WorldRole.Observer)]
    public void WorldRole_Parse_ParsesCorrectly(string name, WorldRole expected)
    {
        var result = Enum.Parse<WorldRole>(name);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("gm", WorldRole.GM)]
    [InlineData("PLAYER", WorldRole.Player)]
    [InlineData("Observer", WorldRole.Observer)]
    public void WorldRole_Parse_IsCaseInsensitive(string name, WorldRole expected)
    {
        var result = Enum.Parse<WorldRole>(name, ignoreCase: true);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WorldRole_Parse_ThrowsOnInvalidValue()
    {
        Assert.Throws<ArgumentException>(() => Enum.Parse<WorldRole>("InvalidRole"));
    }

    [Theory]
    [InlineData(WorldRole.GM, "GM")]
    [InlineData(WorldRole.Player, "Player")]
    [InlineData(WorldRole.Observer, "Observer")]
    public void WorldRole_ToString_ReturnsCorrectName(WorldRole value, string expected)
    {
        Assert.Equal(expected, value.ToString());
    }

    [Theory]
    [InlineData(0, WorldRole.GM)]
    [InlineData(1, WorldRole.Player)]
    [InlineData(2, WorldRole.Observer)]
    public void WorldRole_CastFromInt_Works(int value, WorldRole expected)
    {
        var result = (WorldRole)value;
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WorldRole_IsDefined_ReturnsTrueForValidValues()
    {
        Assert.True(Enum.IsDefined(typeof(WorldRole), WorldRole.GM));
        Assert.True(Enum.IsDefined(typeof(WorldRole), WorldRole.Player));
        Assert.True(Enum.IsDefined(typeof(WorldRole), WorldRole.Observer));
    }

    [Fact]
    public void WorldRole_IsDefined_ReturnsFalseForInvalidValues()
    {
        Assert.False(Enum.IsDefined(typeof(WorldRole), 3));
        Assert.False(Enum.IsDefined(typeof(WorldRole), -1));
        Assert.False(Enum.IsDefined(typeof(WorldRole), 99));
    }

    [Fact]
    public void WorldRole_DefaultValue_IsGM()
    {
        var defaultValue = default(WorldRole);
        Assert.Equal(WorldRole.GM, defaultValue);
        Assert.Equal(0, (int)defaultValue);
    }

    [Theory]
    [InlineData("GM", true)]
    [InlineData("Player", true)]
    [InlineData("Observer", true)]
    [InlineData("InvalidRole", false)]
    [InlineData("", false)]
    public void WorldRole_TryParse_WorksCorrectly(string name, bool shouldSucceed)
    {
        var result = Enum.TryParse<WorldRole>(name, out var value);
        Assert.Equal(shouldSucceed, result);
        
        if (shouldSucceed)
        {
            Assert.True(Enum.IsDefined(typeof(WorldRole), value));
        }
    }

    [Fact]
    public void WorldRole_AllValuesCovered()
    {
        // Ensure no gaps in the sequence 0, 1, 2
        var values = Enum.GetValues<WorldRole>().Select(v => (int)v).OrderBy(v => v).ToList();
        
        Assert.Equal(0, values[0]);
        Assert.Equal(1, values[1]);
        Assert.Equal(2, values[2]);
        
        // No gaps
        for (int i = 0; i < values.Count - 1; i++)
        {
            Assert.Equal(1, values[i + 1] - values[i]);
        }
    }

    [Theory]
    [InlineData(WorldRole.GM, true, true, true)]
    [InlineData(WorldRole.Player, false, true, true)]
    [InlineData(WorldRole.Observer, false, false, true)]
    public void WorldRole_PermissionLevels_AreCorrect(
        WorldRole role, 
        bool canManageWorld, 
        bool canEdit, 
        bool canRead)
    {
        // GM has full permissions
        // Player can edit but not manage
        // Observer is read-only
        
        var actualCanManage = role == WorldRole.GM;
        var actualCanEdit = role == WorldRole.GM || role == WorldRole.Player;
        var actualCanRead = true; // All roles can read
        
        Assert.Equal(canManageWorld, actualCanManage);
        Assert.Equal(canEdit, actualCanEdit);
        Assert.Equal(canRead, actualCanRead);
    }

    [Fact]
    public void WorldRole_IsOrderedByPermissionLevel()
    {
        // Verify that enum values are ordered from most to least permissive
        // GM (most) > Player > Observer (least)
        Assert.True((int)WorldRole.GM < (int)WorldRole.Player);
        Assert.True((int)WorldRole.Player < (int)WorldRole.Observer);
    }

    [Theory]
    [InlineData(WorldRole.GM, WorldRole.Player, true)]
    [InlineData(WorldRole.GM, WorldRole.Observer, true)]
    [InlineData(WorldRole.Player, WorldRole.Observer, true)]
    [InlineData(WorldRole.Player, WorldRole.GM, false)]
    [InlineData(WorldRole.Observer, WorldRole.GM, false)]
    public void WorldRole_Comparison_ReflectsPermissionHierarchy(
        WorldRole more, 
        WorldRole less, 
        bool moreIsMore)
    {
        // Lower numeric value = more permissions
        Assert.Equal(moreIsMore, (int)more < (int)less);
    }

    [Fact]
    public void WorldRole_OnlyGMCanManage()
    {
        var managementRoles = Enum.GetValues<WorldRole>()
            .Where(r => r == WorldRole.GM)
            .ToList();
        
        Assert.Single(managementRoles);
        Assert.Equal(WorldRole.GM, managementRoles[0]);
    }

    [Fact]
    public void WorldRole_OnlyObserverIsReadOnly()
    {
        var readOnlyRoles = Enum.GetValues<WorldRole>()
            .Where(r => r == WorldRole.Observer)
            .ToList();
        
        Assert.Single(readOnlyRoles);
        Assert.Equal(WorldRole.Observer, readOnlyRoles[0]);
    }

    [Theory]
    [InlineData(WorldRole.GM, false)]
    [InlineData(WorldRole.Player, false)]
    [InlineData(WorldRole.Observer, true)]
    public void WorldRole_IsReadOnly_OnlyTrueForObserver(WorldRole role, bool isReadOnly)
    {
        var actuallyReadOnly = role == WorldRole.Observer;
        Assert.Equal(isReadOnly, actuallyReadOnly);
    }
}
