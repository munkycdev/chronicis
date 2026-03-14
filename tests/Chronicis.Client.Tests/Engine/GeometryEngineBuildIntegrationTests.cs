using System.Text.Json;
using Xunit;

namespace Chronicis.Client.Tests.Engine;

public class GeometryEngineBuildIntegrationTests
{
    [Fact]
    public void ClientProject_ReferencesEngineProject()
    {
        var clientProject = File.ReadAllText(Path.Combine(GetRepoRoot(), "src", "Chronicis.Client", "Chronicis.Client.csproj"));

        Assert.Contains(@"..\Chronicis.Client.Engine\Chronicis.Client.Engine.csproj", clientProject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EngineProject_ContainsExpectedStaticAsset()
    {
        var assetPath = Path.Combine(
            GetRepoRoot(),
            "src",
            "Chronicis.Client.Engine",
            "wwwroot",
            "js",
            "chronicis-map-engine.js");

        Assert.True(File.Exists(assetPath));
    }

    [Fact]
    public void EngineProject_ProducesStaticWebAssetManifest()
    {
        var manifestPath = Path.Combine(
            GetRepoRoot(),
            "src",
            "Chronicis.Client",
            "bin",
            "Debug",
            "net9.0",
            "Chronicis.Client.staticwebassets.runtime.json");

        Assert.True(File.Exists(manifestPath));

        using var payload = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var text = payload.RootElement.GetRawText();
        Assert.Contains("chronicis-map-engine.js", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientBuildOutput_IncludesEngineArtifact()
    {
        var manifestPath = Path.Combine(
            GetRepoRoot(),
            "src",
            "Chronicis.Client",
            "obj",
            "Debug",
            "net9.0",
            "staticwebassets.build.json");

        Assert.True(File.Exists(manifestPath));

        using var payload = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var text = payload.RootElement.GetRawText();
        Assert.Contains("chronicis-map-engine.js", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientBuildOutput_CopiesEngineScriptToClientWwwroot()
    {
        var scriptPath = Path.Combine(
            GetRepoRoot(),
            "src",
            "Chronicis.Client",
            "bin",
            "Debug",
            "net9.0",
            "wwwroot",
            "js",
            "chronicis-map-engine.js");

        Assert.True(File.Exists(scriptPath));
    }

    private static string GetRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "Chronicis.sln")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
