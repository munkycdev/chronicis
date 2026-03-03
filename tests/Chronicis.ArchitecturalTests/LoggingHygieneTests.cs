using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Chronicis.ArchitecturalTests;

[ExcludeFromCodeCoverage]
public sealed class LoggingHygieneTests
{
    private static readonly string RepoRoot = ResolveRepoRoot();
    private static readonly Regex DebugOrInformationPattern = new(@"\.\s*Log(?:Debug|Information)(?:Sanitized)?\s*\(", RegexOptions.CultureInvariant);
    private static readonly Regex UnsanitizedApiLogPattern = new(@"\.\s*Log(?:Warning|Error|Critical|Trace)\s*\(", RegexOptions.CultureInvariant);

    [Fact]
    public void ApiSource_MustNotUseDebugOrInformationLogging()
    {
        var files = EnumerateApiSourceFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (DebugOrInformationPattern.IsMatch(content))
            {
                violations.Add(ToRepoRelativePath(file));
            }
        }

        Assert.True(
            violations.Count == 0,
            "API source must not contain debug/information logging calls." + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    [Fact]
    public void ApiSource_MustUseSanitizedLoggerExtensions()
    {
        var files = EnumerateApiSourceFiles();
        var violations = new List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            if (UnsanitizedApiLogPattern.IsMatch(content))
            {
                violations.Add(ToRepoRelativePath(file));
            }
        }

        Assert.True(
            violations.Count == 0,
            "API source contains non-sanitized logger calls." + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    private static IEnumerable<string> EnumerateApiSourceFiles()
    {
        var apiRoot = Path.Combine(RepoRoot, "src", "Chronicis.Api");
        foreach (var file in Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsBuildArtifact(file) || IsInDirectory(file, "Migrations"))
            {
                continue;
            }

            yield return file;
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
}
