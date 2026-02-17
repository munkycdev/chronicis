using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Extensions;
using Chronicis.Shared.Utilities;

namespace Chronicis.ArchitecturalTests;

/// <summary>
/// Unified architectural tests that enforce design patterns and conventions across all projects.
/// Uses reflection to discover assemblies and apply rules consistently.
/// </summary>

[ExcludeFromCodeCoverage]
public class ArchitecturalConventionTests
{
    private static readonly Assembly SharedAssembly = typeof(ArticleDto).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Api.Services.ArticleService).Assembly;
    private static readonly Assembly ClientAssembly = typeof(Client.Services.TreeStateService).Assembly;

    // ════════════════════════════════════════════════════════════════
    //  SERVICE CONVENTIONS (API and Client)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Chronicis.Api.Services", typeof(Api.Services.ArticleService))]
    [InlineData("Chronicis.Client.Services", typeof(Client.Services.TreeStateService))]
    public void Services_MustHaveMatchingInterface(string serviceNamespace, Type sampleType)
    {
        var serviceTypes = sampleType.Assembly.GetTypes()
            .Where(t => t.Namespace == serviceNamespace)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Service"))
            .ToList();

        var violations = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var interfaceName = "I" + serviceType.Name;
            var hasInterface = serviceType.GetInterfaces()
                .Any(i => i.Name == interfaceName);

            if (!hasInterface)
            {
                violations.Add($"{serviceType.Name} must implement interface {interfaceName}");
            }
        }

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Chronicis.Api.Services", typeof(Api.Services.IArticleService))]
    [InlineData("Chronicis.Client.Services", typeof(Client.Services.ITreeStateService))]
    public void ServiceInterfaces_MustStartWithI_Prefix(string serviceNamespace, Type sampleType)
    {
        var interfaces = sampleType.Assembly.GetTypes()
            .Where(t => t.Namespace == serviceNamespace)
            .Where(t => t.IsInterface)
            .ToList();

        var violators = interfaces
            .Where(i => !i.Name.StartsWith("I"))
            .Select(i => i.Name)
            .ToList();

        Assert.Empty(violators);
    }

    [Theory]
    [InlineData("Chronicis.Api.Services", typeof(Api.Services.ArticleService))]
    [InlineData("Chronicis.Client.Services", typeof(Client.Services.TreeStateService))]
    public void ServiceMethods_ReturningTask_MustEndWithAsync(string serviceNamespace, Type sampleType)
    {
        var serviceTypes = sampleType.Assembly.GetTypes()
            .Where(t => t.Namespace == serviceNamespace)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.EndsWith("Service"))
            .ToList();

        var violations = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
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

    // ════════════════════════════════════════════════════════════════
    //  CLIENT-SPECIFIC SERVICE CONVENTIONS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ClientApiServices_MustEndWithApiService()
    {
        var apiServiceTypes = ClientAssembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Client.Services")
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.Name.Contains("Api"))
            .ToList();

        var violations = apiServiceTypes
            .Where(t => !t.Name.EndsWith("ApiService"))
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(violations);
    }

    // ════════════════════════════════════════════════════════════════
    //  API-SPECIFIC SERVICE CONVENTIONS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ApiQuestServices_MustReturnServiceResult()
    {
        var questServiceTypes = ApiAssembly.GetTypes()
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

    // ════════════════════════════════════════════════════════════════
    //  EXTENSION CLASS CONVENTIONS (Shared and Client)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Chronicis.Shared.Extensions", typeof(LoggerExtensions))]
    [InlineData("Chronicis.Client.Extensions", typeof(Client.Extensions.ServiceCollectionExtensions))]
    public void Extensions_ClassesMustBeStatic(string extensionNamespace, Type sampleType)
    {
        var extensionTypes = sampleType.Assembly.GetTypes()
            .Where(t => t.Namespace == extensionNamespace)
            .Where(t => t.IsClass)
            .Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
            .ToList();

        // Also check Client Services namespace for HttpClientExtensions
        if (extensionNamespace == "Chronicis.Client.Extensions")
        {
            var serviceExtensions = ClientAssembly.GetTypes()
                .Where(t => t.Namespace == "Chronicis.Client.Services")
                .Where(t => t.IsClass && t.Name.EndsWith("Extensions"))
                .ToList();
            extensionTypes = extensionTypes.Concat(serviceExtensions).ToList();
        }

        var violations = extensionTypes
            .Where(t => !t.IsAbstract || !t.IsSealed) // Static classes are abstract and sealed
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Chronicis.Shared.Extensions", typeof(LoggerExtensions))]
    [InlineData("Chronicis.Client.Extensions", typeof(Client.Extensions.ServiceCollectionExtensions))]
    public void Extensions_ClassesMustEndWithExtensions(string extensionNamespace, Type sampleType)
    {
        var extensionTypes = sampleType.Assembly.GetTypes()
            .Where(t => t.Namespace == extensionNamespace)
            .Where(t => t.IsClass)
            .Where(t => !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
            .ToList();

        var violations = extensionTypes
            .Where(t => !t.Name.EndsWith("Extensions"))
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Chronicis.Shared.Extensions", typeof(LoggerExtensions))]
    [InlineData("Chronicis.Client.Extensions", typeof(Client.Extensions.ServiceCollectionExtensions))]
    public void Extensions_MethodsMustBeStatic(string extensionNamespace, Type sampleType)
    {
        var extensionTypes = sampleType.Assembly.GetTypes()
            .Where(t => t.Namespace == extensionNamespace ||
                       (t.Namespace == "Chronicis.Client.Services" && t.Name.EndsWith("Extensions")))
            .Where(t => t.IsClass)
            .ToList();

        var violations = new List<string>();

        foreach (var extensionType in extensionTypes)
        {
            var publicMethods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .ToList();

            foreach (var method in publicMethods)
            {
                violations.Add($"{extensionType.Name}.{method.Name} is not static");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void SharedExtensions_FirstParameterMustHaveThis()
    {
        var extensionTypes = SharedAssembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Extensions")
            .Where(t => t.IsClass)
            .ToList();

        var violations = new List<string>();

        foreach (var extensionType in extensionTypes)
        {
            var publicMethods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .ToList();

            foreach (var method in publicMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    violations.Add($"{extensionType.Name}.{method.Name} has no parameters (extension methods require at least one)");
                    continue;
                }

                if (!method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                {
                    violations.Add($"{extensionType.Name}.{method.Name} first parameter is missing 'this' modifier");
                }
            }
        }

        Assert.Empty(violations);
    }

    // ════════════════════════════════════════════════════════════════
    //  UTILITY CLASS CONVENTIONS (Shared and Client)
    // ════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData("Chronicis.Shared.Utilities", typeof(SlugGenerator))]
    [InlineData("Chronicis.Client.Utilities", typeof(Client.Utilities.JsUtilities))]
    public void Utilities_MustBeStatic(string utilityNamespace, Type sampleType)
    {
        var utilityTypes = sampleType.Assembly.GetTypes()
            .Where(t => t.Namespace == utilityNamespace)
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var violations = utilityTypes
            .Where(t => !t.IsAbstract || !t.IsSealed)
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("Chronicis.Shared.Utilities", typeof(SlugGenerator))]
    [InlineData("Chronicis.Client.Utilities", typeof(Client.Utilities.JsUtilities))]
    public void Utilities_MethodsMustBeStatic(string utilityNamespace, Type sampleType)
    {
        var utilityTypes = sampleType.Assembly.GetTypes()
            .Where(t => t.Namespace == utilityNamespace)
            .Where(t => t.IsClass)
            .ToList();

        var violations = new List<string>();

        foreach (var utilityType in utilityTypes)
        {
            var publicMethods = utilityType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .ToList();

            foreach (var method in publicMethods)
            {
                violations.Add($"{utilityType.Name}.{method.Name} is not static");
            }
        }

        Assert.Empty(violations);
    }

    // ════════════════════════════════════════════════════════════════
    //  DTO CONVENTIONS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DTOs_MustEndWithDto_OrAllowedExceptions()
    {
        var dtoTypes = SharedAssembly.GetTypes()
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

    [Fact]
    public void DTOs_MustHaveParameterlessConstructor()
    {
        var dtoTypes = SharedAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Chronicis.Shared.DTOs"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var dtoType in dtoTypes)
        {
            var parameterlessConstructor = dtoType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (parameterlessConstructor == null)
            {
                violations.Add($"{dtoType.Name} does not have a public parameterless constructor (required for JSON serialization)");
            }
        }

        Assert.Empty(violations);
    }

    // ════════════════════════════════════════════════════════════════
    //  MODEL CONVENTIONS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Models_IdProperties_MustBeGuid()
    {
        var modelTypes = SharedAssembly.GetTypes()
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
        var modelTypes = SharedAssembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Models")
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var modelType in modelTypes)
        {
            var createdAt = modelType.GetProperty("CreatedAt");
            if (createdAt != null)
            {
                Assert.True(
                    createdAt.PropertyType == typeof(DateTime) || createdAt.PropertyType == typeof(DateTimeOffset),
                    $"{modelType.Name}.CreatedAt should be DateTime or DateTimeOffset");
            }

            var modifiedAt = modelType.GetProperty("ModifiedAt");
            if (modifiedAt != null)
            {
                Assert.True(
                    modifiedAt.PropertyType == typeof(DateTime?) || modifiedAt.PropertyType == typeof(DateTimeOffset?),
                    $"{modelType.Name}.ModifiedAt should be DateTime? or DateTimeOffset?");
            }

            var createdBy = modelType.GetProperty("CreatedBy");
            if (createdBy != null)
            {
                Assert.True(
                    createdBy.PropertyType == typeof(Guid) || createdBy.PropertyType == typeof(Guid?),
                    $"{modelType.Name}.CreatedBy should be Guid or Guid?");
            }

            var lastModifiedBy = modelType.GetProperty("LastModifiedBy");
            if (lastModifiedBy != null)
            {
                Assert.Equal(typeof(Guid?), lastModifiedBy.PropertyType);
            }
        }
    }

    [Fact]
    public void Models_MustUseGuid_NotInt_ForPrimaryKeys()
    {
        var modelTypes = SharedAssembly.GetTypes()
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

    [Fact]
    public void Models_MustHaveParameterlessConstructor()
    {
        var modelTypes = SharedAssembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Models")
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var modelType in modelTypes)
        {
            var parameterlessConstructor = modelType.GetConstructor(
                BindingFlags.Public | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);

            if (parameterlessConstructor == null)
            {
                violations.Add($"{modelType.Name} does not have a public parameterless constructor (required for EF Core)");
            }
        }

        Assert.Empty(violations);
    }

    // ════════════════════════════════════════════════════════════════
    //  ENUM CONVENTIONS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Enums_MustNotHaveNoneOrDefault_AsZero()
    {
        var enumTypes = SharedAssembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Enums")
            .Where(t => t.IsEnum)
            .ToList();

        foreach (var enumType in enumTypes)
        {
            var values = Enum.GetValues(enumType);
            if (values.Length > 0)
            {
                var zeroName = Enum.GetName(enumType, values.GetValue(0)!);
                Assert.False(
                    zeroName == "None" || zeroName == "Default",
                    $"Enum {enumType.Name} should not have 'None' or 'Default' as zero value. Use meaningful business values.");
            }
        }
    }

    [Fact]
    public void Enums_MustHaveAtLeastOneValue()
    {
        var enumTypes = SharedAssembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Enums")
            .Where(t => t.IsEnum)
            .ToList();

        var violations = new List<string>();

        foreach (var enumType in enumTypes)
        {
            var values = Enum.GetValues(enumType);
            if (values.Length == 0)
            {
                violations.Add($"{enumType.Name} has no values");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Enums_MustNotHaveDuplicateValues()
    {
        var enumTypes = SharedAssembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Shared.Enums")
            .Where(t => t.IsEnum)
            .ToList();

        var violations = new List<string>();

        foreach (var enumType in enumTypes)
        {
            var values = Enum.GetValues(enumType);
            var intValues = values.Cast<object>().Select(v => Convert.ToInt32(v)).ToList();
            var duplicates = intValues.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (duplicates.Any())
            {
                violations.Add($"{enumType.Name} has duplicate values: {string.Join(", ", duplicates)}");
            }
        }

        Assert.Empty(violations);
    }

    // ════════════════════════════════════════════════════════════════
    //  CLIENT-SPECIFIC CONVENTIONS
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ClientPageComponents_MustBeInPagesNamespace()
    {
        var pageTypes = ClientAssembly.GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("Chronicis.Client.Pages"))
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var violations = pageTypes
            .Where(t => !t.Namespace!.Equals("Chronicis.Client.Pages") &&
                       !t.Namespace!.StartsWith("Chronicis.Client.Pages."))
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void ClientServiceExtensions_MustBeStatic()
    {
        var extensionTypes = ClientAssembly.GetTypes()
            .Where(t => t.Namespace == "Chronicis.Client.Extensions")
            .Where(t => t.IsClass)
            .Where(t => t.Name.Contains("ServiceExtensions") || t.Name.Contains("ServiceCollectionExtensions"))
            .ToList();

        var violations = extensionTypes
            .Where(t => !t.IsAbstract || !t.IsSealed)
            .Select(t => t.Name)
            .ToList();

        Assert.Empty(violations);
    }
}
