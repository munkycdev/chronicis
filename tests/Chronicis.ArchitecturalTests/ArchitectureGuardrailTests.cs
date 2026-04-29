using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Chronicis.ArchitecturalTests;

[ExcludeFromCodeCoverage]
public sealed class ArchitectureGuardrailTests
{
    private static readonly Assembly ApiAssembly = typeof(Api.Services.ArticleService).Assembly;
    private static readonly string RepoRoot = ResolveRepoRoot();

    [Fact]
    public void ApiControllers_MustNotDependOnChronicisDbContext()
    {
        var dbContextType = typeof(Api.Data.ChronicisDbContext);
        var controllerBaseType = typeof(Microsoft.AspNetCore.Mvc.ControllerBase);

        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Namespace == "Chronicis.Api.Controllers")
            .Where(t => controllerBaseType.IsAssignableFrom(t))
            .ToList();

        var constructorViolations = new List<string>();
        var fieldViolations = new List<string>();
        var propertyViolations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            foreach (var ctor in controller.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ctor.GetParameters().Any(p => p.ParameterType == dbContextType))
                {
                    constructorViolations.Add(controller.FullName ?? controller.Name);
                }
            }

            foreach (var field in controller.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == dbContextType)
                {
                    fieldViolations.Add($"{controller.FullName}.{field.Name}");
                }
            }

            foreach (var property in controller.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType == dbContextType)
                {
                    propertyViolations.Add($"{controller.FullName}.{property.Name}");
                }
            }
        }

        var violations = constructorViolations
            .Concat(fieldViolations)
            .Concat(propertyViolations)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "API controllers must not directly depend on ChronicisDbContext." + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    [Fact]
    public void ApiControllerSource_MustNotReferenceChronicisDbContext()
    {
        var controllersPath = Path.Combine(RepoRoot, "src", "Chronicis.Api", "Controllers");
        var files = Directory.EnumerateFiles(controllersPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        foreach (var file in files)
        {
            if (IsBuildArtifact(file))
            {
                continue;
            }

            var content = File.ReadAllText(file);
            if (content.Contains("ChronicisDbContext", StringComparison.Ordinal))
            {
                violations.Add(ToRepoRelativePath(file));
            }
        }

        Assert.True(
            violations.Count == 0,
            "Controller source files reference ChronicisDbContext." + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    [Fact]
    public void KeyReadServices_MustDependOnReadAccessPolicyService()
    {
        var readPolicyType = typeof(Api.Services.IReadAccessPolicyService);
        var serviceTypes = new[]
        {
            typeof(Api.Services.PublicWorldService),
            typeof(Api.Services.ArticleService),
            typeof(Api.Services.ArticleDataAccessService),
            typeof(Api.Services.SummaryAccessService),
            typeof(Api.Services.SearchReadService)
        };

        var violations = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var constructorHasReadPolicy = serviceType
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Any(c => c.GetParameters().Any(p => p.ParameterType == readPolicyType));

            if (!constructorHasReadPolicy)
            {
                violations.Add($"{serviceType.FullName} does not inject IReadAccessPolicyService.");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Key read services must inject IReadAccessPolicyService." + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    [Fact]
    public void PublicAndAuthenticatedPathServices_MustUseSharedParitySeams()
    {
        var articleServicePath = Path.Combine(RepoRoot, "src", "Chronicis.Api", "Services", "ArticleService.cs");
        var publicWorldServicePath = Path.Combine(RepoRoot, "src", "Chronicis.Api", "Services", "PublicWorldService.cs");

        var articleServiceContent = File.ReadAllText(articleServicePath);
        var publicWorldServiceContent = File.ReadAllText(publicWorldServicePath);

        Assert.Contains("ArticleSlugPathResolver.ResolveAsync", articleServiceContent, StringComparison.Ordinal);
        Assert.Contains("ArticleReadModelProjection.ArticleDetail", articleServiceContent, StringComparison.Ordinal);
        Assert.Contains("ArticleSlugPathResolver.ResolveAsync", publicWorldServiceContent, StringComparison.Ordinal);
        Assert.Contains("ArticleReadModelProjection.ArticleDetail", publicWorldServiceContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientViewModels_MustNotNavigateToLegacyCampaignOrSessionRoutes()
    {
        var viewModelsPath = Path.Combine(RepoRoot, "src", "Chronicis.Client", "ViewModels");
        var files = Directory.EnumerateFiles(viewModelsPath, "*.cs", SearchOption.AllDirectories);
        var violations = new List<string>();

        var legacyPatterns = new[]
        {
            "NavigateTo($\"/campaign/",
        };

        foreach (var file in files)
        {
            if (IsBuildArtifact(file))
                continue;

            var content = File.ReadAllText(file);
            foreach (var pattern in legacyPatterns)
            {
                if (content.Contains(pattern, StringComparison.Ordinal))
                    violations.Add($"{ToRepoRelativePath(file)}: contains '{pattern}'");
            }
        }

        Assert.True(
            violations.Count == 0,
            "ViewModels must not navigate to legacy /campaign/ or /session/ routes — use IAppNavigator.GoTo*Async() instead." + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    [Fact]
    public void SessionService_MustNotReintroduceLegacySessionArticleWrites()
    {
        var sessionServicePath = Path.Combine(RepoRoot, "src", "Chronicis.Api", "Services", "SessionService.cs");
        var content = File.ReadAllText(sessionServicePath);

        var legacySessionPattern = new Regex(@"\bArticleType\.Session\b", RegexOptions.CultureInvariant);
        Assert.True(!legacySessionPattern.IsMatch(content), "SessionService contains legacy ArticleType.Session reference.");
        Assert.Contains("ArticleType.SessionNote", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientPages_MustNotContainGuidBasedPageDirectives()
    {
        var pagesPath = Path.Combine(RepoRoot, "src", "Chronicis.Client", "Pages");
        var files = Directory.EnumerateFiles(pagesPath, "*.razor", SearchOption.AllDirectories);

        var legacyPrefixes = new[] { "/world/", "/campaign/", "/arc/", "/session/", "/article/", "/w/" };
        var pageDirectivePattern = new Regex(@"^\s*@page\s+""([^""]+)""", RegexOptions.Multiline | RegexOptions.CultureInvariant);

        var violations = new List<string>();

        foreach (var file in files)
        {
            if (IsBuildArtifact(file))
                continue;

            var content = File.ReadAllText(file);
            foreach (Match match in pageDirectivePattern.Matches(content))
            {
                var route = match.Groups[1].Value;
                if (legacyPrefixes.Any(p => route.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    violations.Add($"{ToRepoRelativePath(file)}: @page \"{route}\"");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Client pages must not use GUID-based legacy routes. Use PathResolver (/{*Path}) instead." + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
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
