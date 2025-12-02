using Microsoft.Azure.Functions.Worker;
using Chronicis.Shared.Models;

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Extension methods for FunctionContext to easily access authenticated user.
/// </summary>
public static class FunctionContextExtensions
{
    /// <summary>
    /// Gets the authenticated user from the function context.
    /// Returns null if not authenticated (e.g., [AllowAnonymous] endpoint).
    /// </summary>
    public static User? GetUser(this FunctionContext context)
    {
        if (context.Items.TryGetValue("User", out var user))
        {
            return user as User;
        }
        return null;
    }

    /// <summary>
    /// Gets the authenticated user from the function context.
    /// Throws if user is not present (use only on authenticated endpoints).
    /// </summary>
    public static User GetRequiredUser(this FunctionContext context)
    {
        return context.GetUser() 
            ?? throw new InvalidOperationException("User not found in context. Ensure this endpoint requires authentication.");
    }

    /// <summary>
    /// Gets the Auth0 user principal from the function context.
    /// Contains raw claims from the JWT token.
    /// </summary>
    public static UserPrincipal? GetUserPrincipal(this FunctionContext context)
    {
        if (context.Items.TryGetValue("UserPrincipal", out var principal))
        {
            return principal as UserPrincipal;
        }
        return null;
    }
}
