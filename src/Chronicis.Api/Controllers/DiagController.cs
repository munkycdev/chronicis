using Chronicis.Api.Infrastructure;
using Datadog.Trace;
using Microsoft.AspNetCore.Mvc;

namespace Chronicis.Api.Controllers;

/// <summary>
/// Diagnostic endpoints for observability verification.
/// These endpoints do NOT require authentication.
/// </summary>
[ApiController]
[Route("diag")]
public class DiagController : ControllerBase
{
    private readonly ILogger<DiagController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public DiagController(
        ILogger<DiagController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// GET /diag/datadog - Datadog APM diagnostic endpoint.
    /// Creates a test span, verifies configuration, and checks agent connectivity.
    /// </summary>
    [HttpGet("datadog")]
    public async Task<IActionResult> GetDatadogStatus()
    {
        var diagnostics = DatadogConfiguration.GetDiagnostics();
        
        // Create a diagnostic span
        using var scope = Tracer.Instance.StartActive("diag.datadog");
        scope.Span.SetTag("diag.type", "health_check");
        
        _logger.LogInformation(
            "Datadog diagnostic check initiated. TraceId={TraceId}, SpanId={SpanId}",
            scope.Span.TraceId,
            scope.Span.SpanId);

        // Run both connectivity checks in parallel
        using var client = _httpClientFactory.CreateClient();
        
        var localhostCheckTask = DatadogConfiguration.CheckAgentConnectivityAsync(
            client, 
            "http://127.0.0.1:8126/");
        
        var datadogAgentCheckTask = DatadogConfiguration.CheckAgentConnectivityAsync(
            client, 
            "http://datadog-agent:8126/");

        await Task.WhenAll(localhostCheckTask, datadogAgentCheckTask);

        var localhostResult = await localhostCheckTask;
        var datadogAgentResult = await datadogAgentCheckTask;

        // Tag span with connectivity results
        scope.Span.SetTag("agent.localhost.status", localhostResult.Status);
        scope.Span.SetTag("agent.datadog-agent.status", datadogAgentResult.Status);
        
        if (localhostResult.Status != "reachable" && datadogAgentResult.Status != "reachable")
        {
            scope.Span.SetTag("error", "true");
            _logger.LogWarning(
                "Datadog agent unreachable via both endpoints. " +
                "localhost: {LocalhostStatus} ({LocalhostMessage}), " +
                "datadog-agent: {DatadogAgentStatus} ({DatadogAgentMessage})",
                localhostResult.Status, localhostResult.Message,
                datadogAgentResult.Status, datadogAgentResult.Message);
        }

        // Check for configuration mismatches
        var configIssues = new List<string>();
        
        if (diagnostics.ExpectedEnvironment != null && 
            !string.Equals(diagnostics.Environment, diagnostics.ExpectedEnvironment, StringComparison.OrdinalIgnoreCase))
        {
            configIssues.Add($"Environment mismatch: expected '{diagnostics.ExpectedEnvironment}' but tracer has '{diagnostics.Environment}'");
        }
        
        if (diagnostics.ExpectedAgentUri != null && 
            !string.Equals(diagnostics.AgentUri, diagnostics.ExpectedAgentUri, StringComparison.OrdinalIgnoreCase))
        {
            configIssues.Add($"AgentUri mismatch: expected '{diagnostics.ExpectedAgentUri}' but tracer has '{diagnostics.AgentUri}'");
        }

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            
            // What we expect based on environment variables
            expectedConfig = new
            {
                source = "DD_* environment variables",
                DD_SERVICE = diagnostics.EnvVars?.DD_SERVICE,
                DD_ENV = diagnostics.EnvVars?.DD_ENV,
                DD_VERSION = diagnostics.EnvVars?.DD_VERSION,
                DD_AGENT_HOST = diagnostics.EnvVars?.DD_AGENT_HOST,
                DD_TRACE_AGENT_PORT = diagnostics.EnvVars?.DD_TRACE_AGENT_PORT,
                DD_LOGS_INJECTION = diagnostics.EnvVars?.DD_LOGS_INJECTION,
                DD_TRACE_ENABLED = diagnostics.EnvVars?.DD_TRACE_ENABLED,
                ASPNETCORE_ENVIRONMENT = diagnostics.EnvVars?.ASPNETCORE_ENVIRONMENT,
                resolvedAgentUri = diagnostics.ExpectedAgentUri,
                resolvedEnvironment = diagnostics.ExpectedEnvironment
            },
            
            // What the tracer is actually using
            actualTracerConfig = new
            {
                source = "Tracer.Instance.Settings",
                serviceName = diagnostics.ServiceName,
                environment = diagnostics.Environment,
                serviceVersion = diagnostics.ServiceVersion,
                agentUri = diagnostics.AgentUri,
                logsInjectionEnabled = diagnostics.LogsInjectionEnabled,
                traceEnabled = diagnostics.TracerEnabled
            },
            
            // Connectivity checks to both possible agent endpoints
            connectivityChecks = new
            {
                localhost = new
                {
                    url = "http://127.0.0.1:8126/info",
                    status = localhostResult.Status,
                    statusCode = localhostResult.StatusCode,
                    message = localhostResult.Message,
                    agentInfo = localhostResult.AgentInfo
                },
                datadogAgent = new
                {
                    url = "http://datadog-agent:8126/info",
                    status = datadogAgentResult.Status,
                    statusCode = datadogAgentResult.StatusCode,
                    message = datadogAgentResult.Message,
                    agentInfo = datadogAgentResult.AgentInfo
                }
            },
            
            // Any detected issues
            configurationIssues = configIssues.Count > 0 ? configIssues : null,
            
            // Current span info for correlation verification
            currentSpan = new
            {
                traceId = scope.Span.TraceId.ToString(),
                spanId = scope.Span.SpanId.ToString()
            }
        });
    }
}
