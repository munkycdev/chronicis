using Datadog.Trace;

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Read-only diagnostic utilities for Datadog APM tracing.
/// 
/// IMPORTANT: This class does NOT configure the tracer. All Datadog configuration
/// is driven exclusively by DD_* environment variables, which are read by the
/// Datadog.Trace automatic instrumentation before application code runs.
/// 
/// This class provides:
/// - Read-only access to environment variables
/// - Read-only access to Tracer.Instance.Settings
/// - Agent connectivity testing
/// 
/// It does NOT:
/// - Call Tracer.Configure()
/// - Mutate TracerSettings
/// - Apply fallback values or defaults
/// - Make assumptions about the runtime environment
/// </summary>
public static class DatadogDiagnostics
{
    /// <summary>
    /// Logs the current Datadog tracer state for verification.
    /// Read-only - does not configure anything.
    /// </summary>
    public static void LogTracerState(Serilog.ILogger logger)
    {
        try
        {
            var envVars = ReadEnvironmentVariables();
            var tracerSettings = Tracer.Instance.Settings;
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to read Datadog tracer state");
        }
    }
    
    /// <summary>
    /// Reads current environment variables (DD_*) without interpretation or fallbacks.
    /// </summary>
    public static DatadogEnvVars ReadEnvironmentVariables()
    {
        return new DatadogEnvVars
        {
            DD_SERVICE = Environment.GetEnvironmentVariable("DD_SERVICE"),
            DD_ENV = Environment.GetEnvironmentVariable("DD_ENV"),
            DD_VERSION = Environment.GetEnvironmentVariable("DD_VERSION"),
            DD_AGENT_HOST = Environment.GetEnvironmentVariable("DD_AGENT_HOST"),
            DD_TRACE_AGENT_PORT = Environment.GetEnvironmentVariable("DD_TRACE_AGENT_PORT"),
            DD_LOGS_INJECTION = Environment.GetEnvironmentVariable("DD_LOGS_INJECTION"),
            DD_TRACE_ENABLED = Environment.GetEnvironmentVariable("DD_TRACE_ENABLED"),
            DD_TRACE_AGENT_URL = Environment.GetEnvironmentVariable("DD_TRACE_AGENT_URL"),
            ASPNETCORE_ENVIRONMENT = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        };
    }
    
    /// <summary>
    /// Reads current tracer settings. Read-only snapshot of Tracer.Instance.Settings.
    /// </summary>
    public static DatadogTracerState ReadTracerState()
    {
        var settings = Tracer.Instance.Settings;
        
        return new DatadogTracerState
        {
            ServiceName = settings.ServiceName,
            Environment = settings.Environment,
            ServiceVersion = settings.ServiceVersion,
            AgentUri = settings.AgentUri?.ToString(),
            LogsInjectionEnabled = settings.LogsInjectionEnabled,
            TraceEnabled = settings.TraceEnabled
        };
    }

    /// <summary>
    /// Attempts to reach a Datadog agent endpoint and returns connectivity status.
    /// </summary>
    public static async Task<AgentConnectivityResult> CheckAgentConnectivityAsync(
        HttpClient httpClient, 
        string agentBaseUrl)
    {
        if (string.IsNullOrEmpty(agentBaseUrl))
        {
            return new AgentConnectivityResult
            {
                Url = "(not provided)",
                Status = "skipped",
                Message = "No URL provided"
            };
        }

        var infoUrl = $"{agentBaseUrl.TrimEnd('/')}/info";
        
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await httpClient.GetAsync(infoUrl, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);
                return new AgentConnectivityResult
                {
                    Url = infoUrl,
                    Status = "reachable",
                    StatusCode = (int)response.StatusCode,
                    AgentInfo = content
                };
            }
            
            return new AgentConnectivityResult
            {
                Url = infoUrl,
                Status = "http_error",
                StatusCode = (int)response.StatusCode,
                Message = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
            };
        }
        catch (HttpRequestException ex)
        {
            return new AgentConnectivityResult
            {
                Url = infoUrl,
                Status = "unreachable",
                Message = ex.Message
            };
        }
        catch (TaskCanceledException)
        {
            return new AgentConnectivityResult
            {
                Url = infoUrl,
                Status = "timeout",
                Message = "Connection timed out after 5 seconds"
            };
        }
        catch (Exception ex)
        {
            return new AgentConnectivityResult
            {
                Url = infoUrl,
                Status = "error",
                Message = ex.Message
            };
        }
    }
}

/// <summary>
/// Raw environment variable values. No interpretation, no fallbacks.
/// </summary>
public record DatadogEnvVars
{
    public string? DD_SERVICE { get; init; }
    public string? DD_ENV { get; init; }
    public string? DD_VERSION { get; init; }
    public string? DD_AGENT_HOST { get; init; }
    public string? DD_TRACE_AGENT_PORT { get; init; }
    public string? DD_TRACE_AGENT_URL { get; init; }
    public string? DD_LOGS_INJECTION { get; init; }
    public string? DD_TRACE_ENABLED { get; init; }
    public string? ASPNETCORE_ENVIRONMENT { get; init; }
}

/// <summary>
/// Read-only snapshot of Tracer.Instance.Settings.
/// </summary>
public record DatadogTracerState
{
    public string? ServiceName { get; init; }
    public string? Environment { get; init; }
    public string? ServiceVersion { get; init; }
    public string? AgentUri { get; init; }
    public bool LogsInjectionEnabled { get; init; }
    public bool TraceEnabled { get; init; }
}

/// <summary>
/// Result of an agent connectivity check.
/// </summary>
public record AgentConnectivityResult
{
    public required string Url { get; init; }
    public required string Status { get; init; }
    public int? StatusCode { get; init; }
    public string? Message { get; init; }
    public string? AgentInfo { get; init; }
}
