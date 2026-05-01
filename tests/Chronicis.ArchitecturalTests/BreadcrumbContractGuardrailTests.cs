using System.Text.RegularExpressions;
using Xunit;

namespace Chronicis.ArchitecturalTests;

/// <summary>
/// Guards the Phase 13 contract: breadcrumbs are display-only;
/// IAppUrlBuilder is the sole source of URL strings.
/// </summary>
public class BreadcrumbContractGuardrailTests
{
    private static readonly string ClientServicesDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "Chronicis.Client", "Services"));

    private static readonly string ClientInfraDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "Chronicis.Client", "Infrastructure"));

    [Fact]
    public void BreadcrumbService_ContainsNoBreadcrumbSlugJoin()
    {
        var file = Path.Combine(ClientServicesDir, "BreadcrumbService.cs");
        Assert.True(File.Exists(file), $"BreadcrumbService.cs not found at: {file}");

        var source = File.ReadAllText(file);

        Assert.DoesNotContain("BuildArticleUrl", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildArticleUrlToIndex", source, StringComparison.Ordinal);
        // Guard against the anti-pattern: joining slugs directly into URL strings.
        // Passing slugs to IAppUrlBuilder is fine; constructing path strings inline is not.
        Assert.DoesNotContain("string.Join", source, StringComparison.Ordinal);
    }

    [Fact]
    public void IBreadcrumbService_DoesNotExposeUrlBuildingMethods()
    {
        var file = Path.Combine(ClientServicesDir, "BreadcrumbService.cs");
        var source = File.ReadAllText(file);

        Assert.DoesNotContain("string BuildArticleUrl", source, StringComparison.Ordinal);
        Assert.DoesNotContain("string BuildArticleUrlToIndex", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppNavigator_ContainsNoWikiSlugFilter()
    {
        var file = Path.Combine(ClientInfraDir, "AppNavigator.cs");
        Assert.True(File.Exists(file), $"AppNavigator.cs not found at: {file}");

        var source = File.ReadAllText(file);

        Assert.DoesNotContain("!= \"wiki\"", source, StringComparison.Ordinal);
    }
}
