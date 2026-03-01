using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Chronicis.ArchitecturalTests;

[ExcludeFromCodeCoverage]
public sealed class SessionModelGuardrailTests
{
    private const int ApiLegacyReferenceBaseline = 6;
    private const int ClientLegacyReferenceBaseline = 8;
    private static readonly string RepoRoot = ResolveRepoRoot();
    private static readonly Regex LegacySessionPattern = new(@"\bArticleType\.Session\b", RegexOptions.CultureInvariant);

    [Fact]
    public void Api_ArticleTypeSessionReferences_MustStayInsideCompatibilityBoundary()
    {
        var scan = ScanLegacySessionReferences(
            relativeRoot: Path.Combine("src", "Chronicis.Api"),
            includeRazorFiles: false,
            excludeMigrations: true);

        var allowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "src/Chronicis.Api/Services/ArticleValidationService.cs",
            "src/Chronicis.Api/Services/PublicWorldService.cs"
        };

        var unexpectedFiles = scan.FilesWithReferenceCounts.Keys
            .Where(path => !allowlist.Contains(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(
            unexpectedFiles.Count == 0,
            "Unexpected API files contain ArticleType.Session:" + Environment.NewLine +
            string.Join(Environment.NewLine, unexpectedFiles.Select(path => $"  - {path}")));

        Assert.True(
            scan.TotalReferences <= ApiLegacyReferenceBaseline,
            $"API ArticleType.Session references increased above baseline {ApiLegacyReferenceBaseline}. " +
            $"Current={scan.TotalReferences}.{Environment.NewLine}{scan.Describe()}");
    }

    [Fact]
    public void Client_ArticleTypeSessionReferences_MustStayInsideCompatibilityBoundary()
    {
        var scan = ScanLegacySessionReferences(
            relativeRoot: Path.Combine("src", "Chronicis.Client"),
            includeRazorFiles: true,
            excludeMigrations: false);

        var allowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "src/Chronicis.Client/Components/Admin/TutorialPageTypes.cs",
            "src/Chronicis.Client/Components/Articles/ArticleMetadataDrawer.razor",
            "src/Chronicis.Client/Components/Quests/QuestDrawer.razor.cs",
            "src/Chronicis.Client/Models/TreeNode.cs",
            "src/Chronicis.Client/ViewModels/PublicWorldPageViewModel.cs",
            "src/Chronicis.Client/Services/Tree/TreeDataBuilder.cs"
        };

        var unexpectedFiles = scan.FilesWithReferenceCounts.Keys
            .Where(path => !allowlist.Contains(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.True(
            unexpectedFiles.Count == 0,
            "Unexpected client files contain ArticleType.Session:" + Environment.NewLine +
            string.Join(Environment.NewLine, unexpectedFiles.Select(path => $"  - {path}")));

        Assert.True(
            scan.TotalReferences <= ClientLegacyReferenceBaseline,
            $"Client ArticleType.Session references increased above baseline {ClientLegacyReferenceBaseline}. " +
            $"Current={scan.TotalReferences}.{Environment.NewLine}{scan.Describe()}");
    }

    private static LegacyReferenceScan ScanLegacySessionReferences(
        string relativeRoot,
        bool includeRazorFiles,
        bool excludeMigrations)
    {
        var rootPath = Path.Combine(RepoRoot, relativeRoot);
        var files = EnumerateSourceFiles(rootPath, includeRazorFiles);
        var filesWithCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (excludeMigrations && IsInDirectory(file, "Migrations"))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            var matchCount = LegacySessionPattern.Matches(content).Count;
            if (matchCount == 0)
            {
                continue;
            }

            filesWithCounts[ToRepoRelativePath(file)] = matchCount;
        }

        return new LegacyReferenceScan(filesWithCounts);
    }

    private static IEnumerable<string> EnumerateSourceFiles(string rootPath, bool includeRazorFiles)
    {
        foreach (var file in Directory.EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories))
        {
            if (!IsBuildArtifact(file))
            {
                yield return file;
            }
        }

        if (!includeRazorFiles)
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(rootPath, "*.razor", SearchOption.AllDirectories))
        {
            if (!IsBuildArtifact(file))
            {
                yield return file;
            }
        }
    }

    private static bool IsBuildArtifact(string filePath)
    {
        return IsInDirectory(filePath, "bin") || IsInDirectory(filePath, "obj");
    }

    private static bool IsInDirectory(string filePath, string directoryName)
    {
        var normalized = filePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var marker = $"{Path.DirectorySeparatorChar}{directoryName}{Path.DirectorySeparatorChar}";
        return normalized.Contains(marker, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToRepoRelativePath(string filePath)
    {
        return Path.GetRelativePath(RepoRoot, filePath).Replace('\\', '/');
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Chronicis.CI.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test execution directory.");
    }

    private sealed record LegacyReferenceScan(IReadOnlyDictionary<string, int> FilesWithReferenceCounts)
    {
        public int TotalReferences => FilesWithReferenceCounts.Values.Sum();

        public string Describe()
        {
            if (FilesWithReferenceCounts.Count == 0)
            {
                return "  - <none>";
            }

            return string.Join(
                Environment.NewLine,
                FilesWithReferenceCounts
                    .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(item => $"  - {item.Key} ({item.Value})"));
        }
    }
}
