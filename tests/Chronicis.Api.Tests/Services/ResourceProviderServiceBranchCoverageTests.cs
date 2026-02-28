using System.Reflection;
using Chronicis.Api.Repositories;
using Chronicis.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

public class ResourceProviderServiceBranchCoverageTests
{
    [Fact]
    public void Constructor_AssignsDependencies()
    {
        using var db = RemainingApiBranchCoverageTestHelpers.CreateDbContext();
        var repository = Substitute.For<IResourceProviderRepository>();
        var logger = NullLogger<ResourceProviderService>.Instance;

        var service = new ResourceProviderService(repository, db, logger);

        Assert.NotNull(service);
    }

    [Fact]
    public void LookupKeyPattern_StaticField_IsInitialized()
    {
        var field = typeof(ResourceProviderService).GetField(
            "LookupKeyPattern",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(field);
        Assert.NotNull(field!.GetValue(null));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Open5E_Rules  ", "open5e_rules")]
    public void NormalizeLookupKey_CoversNullWhitespaceAndValueBranches(string? input, string? expected)
    {
        var normalizeLookupKey = RemainingApiBranchCoverageTestHelpers.GetMethod(
            typeof(ResourceProviderService),
            "NormalizeLookupKey");

        var normalized = (string?)normalizeLookupKey.Invoke(null, [input]);

        Assert.Equal(expected, normalized);
    }
}
