using Chronicis.Api.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Reflection;

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Middleware that handles JWT authentication globally for all HTTP-triggered functions.
/// Functions marked with [AllowAnonymous] skip authentication.
/// Authenticated user is available via context.Items["User"].
/// </summary>
public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    private readonly ILogger<AuthenticationMiddleware> _logger;
    private readonly Auth0Configuration _auth0Config;
    private readonly IServiceProvider _serviceProvider;

    public AuthenticationMiddleware(
        ILogger<AuthenticationMiddleware> logger,
        IOptions<Auth0Configuration> auth0Config,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _auth0Config = auth0Config.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // Check if this is an HTTP trigger
        var httpRequestData = await context.GetHttpRequestDataAsync();
        if (httpRequestData == null)
        {
            // Not an HTTP trigger (timer, queue, etc.) - skip auth
            await next(context);
            return;
        }

        // Check if function has [AllowAnonymous] attribute
        if (HasAllowAnonymousAttribute(context))
        {
            _logger.LogDebug("Skipping authentication for {FunctionName} (AllowAnonymous)",
                context.FunctionDefinition.Name);
            await next(context);
            return;
        }

        // Validate JWT and get user principal
        var principal = await Auth0AuthenticationHelper.GetUserFromTokenAsync(
            httpRequestData,
            _auth0Config.Domain,
            _auth0Config.Audience);

        if (principal == null)
        {
            _logger.LogWarning("Authentication failed for {FunctionName}: No valid token",
                context.FunctionDefinition.Name);
            await SetUnauthorizedResponse(context, httpRequestData, "Authentication required. Please provide a valid Auth0 token.");
            return;
        }

        // Get or create user in database
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            var user = await userService.GetOrCreateUserAsync(
                principal.Auth0UserId,
                principal.Email,
                principal.DisplayName,
                principal.AvatarUrl);

            // Store user in context for functions to access
            context.Items["User"] = user;
            context.Items["UserPrincipal"] = principal;

            _logger.LogInformation("Authenticated user {UserId} ({Email}) for {FunctionName}",
                user.Id, user.Email, context.FunctionDefinition.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user lookup for Auth0 ID: {Auth0UserId}", principal.Auth0UserId);
            await SetUnauthorizedResponse(context, httpRequestData, "An error occurred during authentication.");
            return;
        }

        // Continue to the function
        await next(context);
    }

    private static bool HasAllowAnonymousAttribute(FunctionContext context)
    {
        // Get the method info for the function being executed
        var entryPoint = context.FunctionDefinition.EntryPoint;

        // EntryPoint format: "Namespace.ClassName.MethodName"
        var lastDotIndex = entryPoint.LastIndexOf('.');
        if (lastDotIndex < 0) return false;

        var typeName = entryPoint.Substring(0, lastDotIndex);
        var methodName = entryPoint.Substring(lastDotIndex + 1);

        // Find the type in loaded assemblies
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => t.FullName == typeName);

        if (type == null) return false;

        // Check for attribute on method
        var method = type.GetMethod(methodName);
        if (method?.GetCustomAttribute<AllowAnonymousAttribute>() != null)
            return true;

        // Check for attribute on class
        if (type.GetCustomAttribute<AllowAnonymousAttribute>() != null)
            return true;

        return false;
    }

    private static async Task SetUnauthorizedResponse(
        FunctionContext context,
        HttpRequestData httpRequestData,
        string message)
    {
        var response = httpRequestData.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new { error = message });

        var invocationResult = context.GetInvocationResult();
        invocationResult.Value = response;
    }
}
