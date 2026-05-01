using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Chronicis.ArchitecturalTests;

[ExcludeFromCodeCoverage]
public sealed class PublicSurfaceGuardrailTests
{
    private static readonly Assembly ApiAssembly = typeof(Api.Services.ArticleService).Assembly;
    private static readonly Assembly SharedAssembly = typeof(Shared.DTOs.ArticleDto).Assembly;

    [Fact]
    public void RetiredPublicSurfaceTypes_MustNotExist()
    {
        var retiredTypeNames = new[]
        {
            "PublicWorldService",
            "IPublicWorldService",
            "PublicController",
            "PublicSlugCheckDto",
            "PublicSlugCheckResultDto",
            "IWorldPublicSharingService",
            "WorldPublicSharingService"
        };

        var violations = new List<string>();

        foreach (var assembly in new[] { ApiAssembly, SharedAssembly })
        {
            foreach (var name in retiredTypeNames)
            {
                var found = assembly.GetTypes().Any(t => t.Name == name);
                if (found)
                {
                    violations.Add($"{name} found in {assembly.GetName().Name}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Retired public surface types must not exist:" + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }

    [Fact]
    public void RetiredReadAccessPolicyMembers_MustNotExist()
    {
        var policyInterface = ApiAssembly.GetType("Chronicis.Api.Services.IReadAccessPolicyService");
        Assert.NotNull(policyInterface);

        var retiredMemberNames = new[] { "NormalizePublicSlug", "ApplyPublicWorldSlugFilter" };
        var violations = new List<string>();

        foreach (var name in retiredMemberNames)
        {
            var exists = policyInterface!
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Any(m => m.Name == name);

            if (exists)
            {
                violations.Add($"IReadAccessPolicyService.{name}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Retired IReadAccessPolicyService members must not exist:" + Environment.NewLine +
            string.Join(Environment.NewLine, violations.Select(v => $"  - {v}")));
    }
}
