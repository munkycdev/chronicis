using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Infrastructure;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class Auth0ConfigurationTests
{
    [Fact]
    public void Constructor_SetsEmptyStringDefaults()
    {
        var sut = new Auth0Configuration();

        Assert.Equal(string.Empty, sut.Domain);
        Assert.Equal(string.Empty, sut.Audience);
        Assert.Equal(string.Empty, sut.ClientId);
    }

    [Fact]
    public void Properties_CanBeAssignedAndReadBack()
    {
        var sut = new Auth0Configuration
        {
            Domain = "dev-chronicis.us.auth0.com",
            Audience = "https://api.chronicis.app",
            ClientId = "client-id-123"
        };

        Assert.Equal("dev-chronicis.us.auth0.com", sut.Domain);
        Assert.Equal("https://api.chronicis.app", sut.Audience);
        Assert.Equal("client-id-123", sut.ClientId);
    }
}
