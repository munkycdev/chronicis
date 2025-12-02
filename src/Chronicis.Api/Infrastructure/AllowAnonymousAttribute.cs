namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Marks a function as allowing anonymous access (no authentication required).
/// Apply this attribute to functions that should be publicly accessible.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class AllowAnonymousAttribute : Attribute
{
}
