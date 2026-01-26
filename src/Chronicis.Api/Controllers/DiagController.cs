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
    /// Creates a test span and checks agent connectivity.
    /// </summary>
    [HttpGet("datadog")]
    public async Task<IActionResult> GetDatadogStatus()
    {
        var diagnostics = DatadogConfiguration.GetDiagnostics();
        string agentStatus;
        string? agentInfo = null;

        // Create a diagnostic span
        using (var scope = Tracer.Instance.StartActive("diag.datadog"))
        {
            scope.Span.SetTag("diag.type", "health_check");
            
            _logger.LogInformation(
                "Datadog diagnostic check initiated. TraceId={TraceId}, SpanId={SpanId}",
                scope.Span.TraceId,
                scope.Span.SpanId);

            // Try to reach the Datadog agent
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var agentUri = diagnostics.AgentUri.TrimEnd('/');
                var response = await client.GetAsync($"{agentUri}/info");
                
                if (response.IsSuccessStatusCode)
                {
                    agentStatus = "reachable";
                    agentInfo = await response.Content.ReadAsStringAsync();
                    scope.Span.SetTag("agent.status", "reachable");
                }
                else
                {
                    agentStatus = $"responded_with_{(int)response.StatusCode}";
                    scope.Span.SetTag("agent.status", agentStatus);
                }
            }
            catch (HttpRequestException ex)
            {
                agentStatus = "unreachable";
                scope.Span.SetTag("agent.status", "unreachable");
                scope.Span.SetTag("error", "true");
                _logger.LogWarning(ex, "Datadog agent unreachable at {AgentUri}", diagnostics.AgentUri);
            }
            catch (TaskCanceledException)
            {
                agentStatus = "timeout";
                scope.Span.SetTag("agent.status", "timeout");
                scope.Span.SetTag("error", "true");
                _logger.LogWarning("Datadog agent connection timed out at {AgentUri}", diagnostics.AgentUri);
            }
        }

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            tracer = new
            {
                diagnostics.ServiceName,
                diagnostics.Environment,
                diagnostics.ServiceVersion,
                diagnostics.AgentUri,
                diagnostics.LogsInjectionEnabled,
                diagnostics.TracerEnabled
            },
            agent = new
            {
                status = agentStatus,
                info = agentInfo
            }
        });
    }
}
