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
        var manifestPath = FindClientBinArtifact(
            "Chronicis.Client.staticwebassets.runtime.json",
            "net9.0");

        Assert.True(File.Exists(manifestPath), manifestPath);

        using var payload = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var text = payload.RootElement.GetRawText();
        Assert.Contains("chronicis-map-engine.js", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientBuildOutput_IncludesEngineArtifact()
    {
        var manifestPath = FindClientObjArtifact(
            "staticwebassets.build.json",
            "net9.0");

        Assert.True(File.Exists(manifestPath), manifestPath);

        using var payload = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var text = payload.RootElement.GetRawText();
        Assert.Contains("chronicis-map-engine.js", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientBuildOutput_CopiesEngineScriptToClientWwwroot()
    {
        var scriptPath = FindClientBinArtifact(
            "chronicis-map-engine.js",
            "net9.0",
            "wwwroot",
            "js");

        Assert.True(File.Exists(scriptPath), scriptPath);
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

    private static string FindClientBinArtifact(string fileName, params string[] requiredSegments) =>
        FindClientArtifact("bin", fileName, requiredSegments);

    private static string FindClientObjArtifact(string fileName, params string[] requiredSegments) =>
        FindClientArtifact("obj", fileName, requiredSegments);

    private static string FindClientArtifact(string rootFolder, string fileName, params string[] requiredSegments)
    {
        var clientRoot = Path.Combine(GetRepoRoot(), "src", "Chronicis.Client", rootFolder);
        var match = Directory
            .EnumerateFiles(clientRoot, fileName, SearchOption.AllDirectories)
            .FirstOrDefault(path => requiredSegments.All(segment =>
                path.Contains($"{Path.DirectorySeparatorChar}{segment}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith($"{Path.DirectorySeparatorChar}{segment}", StringComparison.OrdinalIgnoreCase)));

        return match ?? Path.Combine(clientRoot, requiredSegments.Aggregate(string.Empty, Path.Combine), fileName);
    }
}
