using System.Diagnostics.CodeAnalysis;

namespace Chronicis.ArchitecturalTests;

[ExcludeFromCodeCoverage]
public sealed class WorldUpdateGuardrailTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Fact]
    public void WorldServiceSourceCode_ContainsNoPublicSlugSymbol()
    {
        var path = Path.Combine(RepoRoot, "src", "Chronicis.Api", "Services", "WorldService.cs");
        Assert.True(File.Exists(path), $"WorldService.cs not found at {path}");

        var source = File.ReadAllText(path);
        Assert.DoesNotContain("PublicSlug", source);
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Chronicis.CI.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test execution directory.");
    }
}
