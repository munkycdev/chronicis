using System.Reflection;
using Chronicis.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Chronicis.Api.Tests;

internal static class RemainingApiBranchCoverageTestHelpers
{
    internal static MethodInfo GetMethod(Type type, string name)
    {
        return type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Method '{name}' not found on {type.FullName}");
    }

    internal static void SetField<T>(object instance, string name, T value)
    {
        var field = instance.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '{name}' not found");
        field.SetValue(instance, value);
    }

    internal static ChronicisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ChronicisDbContext>()
            .UseInMemoryDatabase($"remaining-coverage-{Guid.NewGuid()}")
            .Options;
        return new ChronicisDbContext(options);
    }
}
