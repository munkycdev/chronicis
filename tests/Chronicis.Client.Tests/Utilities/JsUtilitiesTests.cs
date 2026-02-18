using Chronicis.Client.Utilities;
using Xunit;

namespace Chronicis.Client.Tests.Utilities;

public class JsUtilitiesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EscapeForJs_ReturnsEmpty_WhenInputIsNullOrEmpty(string? input)
    {
        var escaped = JsUtilities.EscapeForJs(input!);

        Assert.Equal(string.Empty, escaped);
    }

    [Fact]
    public void EscapeForJs_EscapesSpecialCharacters()
    {
        var input = "a\\b'c\"d\ne\rf";

        var escaped = JsUtilities.EscapeForJs(input);

        Assert.Equal("a\\\\b\\'c\\\"d\\ne\\rf", escaped);
    }
}

