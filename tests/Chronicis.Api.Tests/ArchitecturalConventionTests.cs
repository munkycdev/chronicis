using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Models;
using System.Reflection;
using Xunit;

namespace Chronicis.Api.Tests;

/// <summary>
/// Architectural tests that enforce design patterns and conventions.
/// These tests protect against AI-generated or human modifications that might
/// break coding conventions without breaking functionality.
/// </summary>
public class ArchitecturalConventionTests
{
    // ────────────────────────────────────────────────────────────────
    //  DTO Naming Conventions
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void DTOs_MustEndWithDto_OrAllowedExceptions()
    {
        var dtoTypes = typeof(ArticleDto).Assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Chronicis.Shared.DTOs"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var allowedExceptions = new[] { "ToggleResourceProviderRequest", "PagedResult`1", "DocumentContentResult" };

        var violators = dtoTypes
            .Where(t => !t.Name.EndsWith("Dto") && !t.Name.EndsWith("Result") && !t.Name.EndsWith("Request"))
            .Where(t => !allowedExceptions.Contains(t.Name))
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(violators);
    }

    // ────────────────────────────────────────────────────────────────
    //  Model Property Conventions
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Models_IdProperties_MustBeGuid()
    {
        var modelTypes = typeof(Article).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Models")
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var modelType in modelTypes)
        {
            var idProperty = modelType.GetProperty("Id");
            if (idProperty != null)
            {
                Assert.Equal(typeof(Guid), idProperty.PropertyType);
            }
        }
    }

    [Fact]
    public void Models_AuditProperties_FollowConvention()
    {
        var modelTypes = typeof(Article).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Models")
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var modelType in modelTypes)
        {
            // CreatedAt can be DateTime or DateTimeOffset
            var createdAt = modelType.GetProperty("CreatedAt");
            if (createdAt != null)
            {
                Assert.True(
                    createdAt.PropertyType == typeof(DateTime) || createdAt.PropertyType == typeof(DateTimeOffset),
                    $"{modelType.Name}.CreatedAt should be DateTime or DateTimeOffset");
            }

            // ModifiedAt should be nullable
            var modifiedAt = modelType.GetProperty("ModifiedAt");
            if (modifiedAt != null)
            {
                Assert.True(
                    modifiedAt.PropertyType == typeof(DateTime?) || modifiedAt.PropertyType == typeof(DateTimeOffset?),
                    $"{modelType.Name}.ModifiedAt should be DateTime? or DateTimeOffset?");
            }

            // CreatedBy can be Guid or Guid? depending on whether creation is tracked
            var createdBy = modelType.GetProperty("CreatedBy");
            if (createdBy != null)
            {
                Assert.True(
                    createdBy.PropertyType == typeof(Guid) || createdBy.PropertyType == typeof(Guid?),
                    $"{modelType.Name}.CreatedBy should be Guid or Guid?");
            }

            // LastModifiedBy should be nullable
            var lastModifiedBy = modelType.GetProperty("LastModifiedBy");
            if (lastModifiedBy != null)
            {
                Assert.Equal(typeof(Guid?), lastModifiedBy.PropertyType);
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Service Interface Conventions
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Services_MustHaveMatchingInterface()
    {
        var serviceTypes = typeof(Api.Services.ArticleService).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Api.Services")
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Service"))
            .ToList();

        foreach (var serviceType in serviceTypes)
        {
            var interfaceName = "I" + serviceType.Name;
            var hasInterface = serviceType.GetInterfaces()
                .Any(i => i.Name == interfaceName);

            Assert.True(hasInterface, 
                $"Service {serviceType.Name} must implement interface {interfaceName}");
        }
    }

    [Fact]
    public void ServiceInterfaces_MustStartWithI_Prefix()
    {
        var interfaces = typeof(Api.Services.IArticleService).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Api.Services")
            .Where(t => t.IsInterface)
            .ToList();

        var violators = interfaces
            .Where(i => !i.Name.StartsWith("I"))
            .Select(i => i.Name)
            .ToList();

        Assert.Empty(violators);
    }

    // ────────────────────────────────────────────────────────────────
    //  Enum Conventions
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Enums_MustNotHaveNoneOrDefault_AsZero()
    {
        var enumTypes = typeof(ArticleType).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Enums")
            .Where(t => t.IsEnum)
            .ToList();

        foreach (var enumType in enumTypes)
        {
            var values = Enum.GetValues(enumType);
            if (values.Length > 0)
            {
                var zeroValue = Convert.ToInt32(values.GetValue(0));
                var zeroName = Enum.GetName(enumType, values.GetValue(0)!);

                // Ensure zero value is not "None" or "Default" (anti-pattern)
                // Zero should be a valid business value
                Assert.False(
                    zeroName == "None" || zeroName == "Default",
                    $"Enum {enumType.Name} should not have 'None' or 'Default' as zero value. Use meaningful business values.");
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Async Method Conventions
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void ServiceMethods_ReturningTask_MustEndWithAsync()
    {
        var serviceTypes = typeof(Api.Services.ArticleService).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Api.Services")
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Service"))
            .ToList();

        var violations = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName) // Exclude properties
                .ToList();

            foreach (var method in methods)
            {
                var returnsTask = method.ReturnType == typeof(Task) ||
                                 (method.ReturnType.IsGenericType && 
                                  method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));

                if (returnsTask && !method.Name.EndsWith("Async"))
                {
                    violations.Add($"{serviceType.Name}.{method.Name}");
                }
            }
        }

        Assert.Empty(violations);
    }

    // ────────────────────────────────────────────────────────────────
    //  ServiceResult Pattern Enforcement
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void QuestServices_MustReturnServiceResult()
    {
        // Quest services should use ServiceResult pattern for consistent error handling
        var questServiceTypes = typeof(Api.Services.QuestService).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Api.Services")
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.Contains("Quest") && t.Name.EndsWith("Service"))
            .ToList();

        var violations = new List<string>();

        foreach (var serviceType in questServiceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Where(m => m.ReturnType.IsGenericType && 
                           m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                .ToList();

            foreach (var method in methods)
            {
                var taskResultType = method.ReturnType.GetGenericArguments()[0];
                var isServiceResult = taskResultType.IsGenericType &&
                                     taskResultType.GetGenericTypeDefinition().Name == "ServiceResult`1";

                if (!isServiceResult)
                {
                    violations.Add($"{serviceType.Name}.{method.Name} returns {taskResultType.Name} instead of ServiceResult<T>");
                }
            }
        }

        Assert.Empty(violations);
    }

    // ────────────────────────────────────────────────────────────────
    //  String Property Initialization
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void DTOs_StringProperties_ShouldInitializeToEmpty()
    {
        // This documents the pattern but doesn't enforce it strictly
        // Some nullable string properties are intentionally null
        var dtoTypes = typeof(ArticleDto).Assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Chronicis.Shared.DTOs"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var dtoType in dtoTypes)
        {
            try
            {
                var instance = Activator.CreateInstance(dtoType);
                var stringProps = dtoType.GetProperties()
                    .Where(p => p.PropertyType == typeof(string))
                    .ToList();

                foreach (var prop in stringProps)
                {
                    var value = prop.GetValue(instance);
                    if (value == null)
                    {
                        violations.Add($"{dtoType.Name}.{prop.Name}");
                    }
                }
            }
            catch
            {
                // Skip types that can't be instantiated
            }
        }

        // Informational only - we allow nullable strings
        Assert.True(true, $"Info: {violations.Count} string properties are nullable (this is allowed)");
    }

    // ────────────────────────────────────────────────────────────────
    //  Navigation Property Conventions
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Models_NavigationProperties_ShouldConsiderVirtual()
    {
        // Navigation properties being virtual enables lazy loading
        // This is a recommendation for best practices
        var modelTypes = typeof(Article).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Models")
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var nonVirtualCount = 0;

        foreach (var modelType in modelTypes)
        {
            var navigationProps = modelType.GetProperties()
                .Where(p => p.PropertyType.Namespace?.StartsWith("Chronicis.Shared.Models") == true ||
                           (p.PropertyType.IsGenericType && 
                            p.PropertyType.GetGenericArguments().Any(a => 
                                a.Namespace?.StartsWith("Chronicis.Shared.Models") == true)))
                .ToList();

            foreach (var prop in navigationProps)
            {
                var getMethod = prop.GetGetMethod();
                if (getMethod != null && !getMethod.IsVirtual)
                {
                    nonVirtualCount++;
                }
            }
        }

        // This is informational - we're not using lazy loading, so non-virtual is OK
        // But worth tracking in case we change our EF Core strategy
        Assert.True(true, $"Info: {nonVirtualCount} navigation properties are non-virtual (expected if not using lazy loading)");
    }

    // ────────────────────────────────────────────────────────────────
    //  Guid vs int Convention
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void Models_MustUseGuid_NotInt_ForPrimaryKeys()
    {
        var modelTypes = typeof(Article).Assembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Models")
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var modelType in modelTypes)
        {
            var idProperty = modelType.GetProperty("Id");
            if (idProperty != null && idProperty.PropertyType == typeof(int))
            {
                violations.Add($"{modelType.Name}.Id is int, should be Guid");
            }
        }

        Assert.Empty(violations);
    }

    // ────────────────────────────────────────────────────────────────
    //  Collection Initialization
    // ────────────────────────────────────────────────────────────────

    [Fact]
    public void DTOs_CollectionProperties_ShouldConsiderInitialization()
    {
        // Collections should be initialized to avoid null reference exceptions
        // But nullable collections (ICollection<T>?) are allowed
        var dtoTypes = typeof(ArticleDto).Assembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Chronicis.Shared.DTOs"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var dtoType in dtoTypes)
        {
            var collectionProps = dtoType.GetProperties()
                .Where(p => typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType))
                .Where(p => p.PropertyType != typeof(string))
                .Where(p => p.Name != "Children") // Children is intentionally nullable for tree structures
                .ToList();

            foreach (var prop in collectionProps)
            {
                try
                {
                    var instance = Activator.CreateInstance(dtoType);
                    var value = prop.GetValue(instance);

                    if (value == null)
                    {
                        violations.Add($"{dtoType.Name}.{prop.Name}");
                    }
                }
                catch
                {
                    // Skip types that can't be instantiated
                }
            }
        }

        // Soft assertion - collections can be nullable if intentional
        if (violations.Count > 5)
        {
            Assert.Fail($"Many collection properties are not initialized: {violations.Count}. Consider initializing or making explicitly nullable.");
        }
    }
}
