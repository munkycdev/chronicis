using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Api.Data;
using Chronicis.Api.Services;
using Chronicis.Api.Services.Routing;
using Xunit;

namespace Chronicis.Api.Tests.Architecture;

[ExcludeFromCodeCoverage]
public class SlugPathResolverArchitectureTests
{
    private static readonly ConstructorInfo Constructor =
        typeof(SlugPathResolver).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();

    [Fact]
    public void SlugPathResolver_DependsOn_IReadAccessPolicyService()
    {
        var paramTypes = Constructor.GetParameters().Select(p => p.ParameterType).ToList();
        Assert.Contains(typeof(IReadAccessPolicyService), paramTypes);
    }

    [Fact]
    public void SlugPathResolver_DependsOn_IReservedSlugProvider()
    {
        var paramTypes = Constructor.GetParameters().Select(p => p.ParameterType).ToList();
        Assert.Contains(typeof(IReservedSlugProvider), paramTypes);
    }

    [Fact]
    public void SlugPathResolver_DoesNotDependOn_ChronicisDbContext()
    {
        var paramTypes = Constructor.GetParameters().Select(p => p.ParameterType).ToList();
        Assert.DoesNotContain(typeof(ChronicisDbContext), paramTypes);
    }
}
