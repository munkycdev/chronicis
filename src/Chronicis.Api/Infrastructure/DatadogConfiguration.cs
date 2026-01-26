using Datadog.Trace;

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Helper for Datadog APM tracing diagnostics and verification.
/// 
/// IMPORTANT: Datadog tracing is configured automatically via DD_* environment variables
/// when the Datadog.Trace package is loaded. This class provides diagnostic utilities
/// to verify configuration is applied correctly - it does NOT configure the tracer.
/// 
/// Required environment variables for production:
/// - DD_AGENT_HOST: Hostname of the Datadog agent (e.g., "datadog-agent")
/// - DD_TRACE_AGENT_PORT: Port of the Datadog agent (default: 8126)
/// - DD_SERVICE: Service name (e.g., "chronicis-api")
/// - DD_ENV: Environment name (e.g., "production", "development")
/// - DD_VERSION: Service version (optional)
/// - DD_LOGS_INJECTION: Enable log correlation (set to "true")
/// - DD_TRACE_ENABLED: Enable tracing (set to "true")
/// </summary>
public static class DatadogConfiguration
{
    /// <summary>
    /// Logs the current Datadog tracer configuration for verification.
    /// Does NOT configure the tracer - configuration is done via DD_* env vars.
    /// </summary>
    public static void LogTracerConfiguration(Serilog.ILogger logger)
    {
        try
        {
            var envVars = GetEnvironmentVariables();
            var tracerSettings = Tracer.Instance.Settings;
            
            logger.Information(
                "Datadog APM configuration (from env vars): " +
                "DD_SERVICE={Service}, DD_ENV={Env}, DD_AGENT_HOST={AgentHost}, " +
                "DD_TRACE_AGENT_PORT={AgentPort}, DD_LOGS_INJECTION={LogsInjection}, DD_TRACE_ENABLED={TraceEnabled}",
                envVars.ServiceName ?? "(not set)",
                envVars.Environment ?? "(not set)",
                envVars.AgentHost ?? "(not set)",
                envVars.AgentPort ?? "(not set)",
                envVars.LogsInjectionEnabled ?? "(not set)",
                envVars.TraceEnabled ?? "(not set)");
            
            logger.Information(
                "Datadog APM actual tracer settings: " +
                "ServiceName={Service}, Environment={Env}, AgentUri={AgentUri}, " +
                "LogsInjectionEnabled={LogsInjection}, TraceEnabled={TraceEnabled}",
                tracerSettings.ServiceName,
                tracerSettings.Environment,
                tracerSettings.AgentUri,
                tracerSettings.LogsInjectionEnabled,
                tracerSettings.TraceEnabled);

            // Warn if env vars don't match tracer settings (indicates configuration issue)
            if (!string.IsNullOrEmpty(envVars.Environment) && 
                !string.Equals(envVars.Environment, tracerSettings.Environment, StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning(
                    "Datadog DD_ENV mismatch: env var={EnvVar}, tracer={TracerEnv}. " +
                    "This may indicate the tracer was configured before env vars were set.",
                    envVars.Environment,
                    tracerSettings.Environment);
            }

            if (!string.IsNullOrEmpty(envVars.AgentHost))
            {
                var expectedUri = BuildAgentUri(envVars.AgentHost, envVars.AgentPort);
                if (tracerSettings.AgentUri != null && 
                    !string.Equals(expectedUri.Host, tracerSettings.AgentUri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warning(
                        "Datadog agent host mismatch: expected={Expected}, actual={Actual}. " +
                        "This may indicate the tracer was configured before env vars were set.",
                        expectedUri,
                        tracerSettings.AgentUri);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Failed to read Datadog tracer configuration");
        }
    }
    
    /// <summary>
    /// Gets current tracer configuration for diagnostics, including both
    /// environment variable values and actual tracer settings.
    /// </summary>
    public static DatadogDiagnostics GetDiagnostics()
    {
        var envVars = GetEnvironmentVariables();
        var tracerSettings = Tracer.Instance.Settings;
        
        // Build expected agent URI from env vars
        var expectedAgentUri = BuildAgentUri(envVars.AgentHost, envVars.AgentPort);
        
        // Determine effective environment with fallback logic
        var effectiveEnv = ResolveEnvironment(envVars.Environment);
        
        return new DatadogDiagnostics
        {
            // Actual tracer settings
            ServiceName = tracerSettings.ServiceName ?? "unknown",
            Environment = tracerSettings.Environment ?? "unknown",
            ServiceVersion = tracerSettings.ServiceVersion ?? "unknown",
            AgentUri = tracerSettings.AgentUri?.ToString() ?? "unknown",
            LogsInjectionEnabled = tracerSettings.LogsInjectionEnabled,
            TracerEnabled = tracerSettings.TraceEnabled,
            
            // Environment variable values (for debugging)
            EnvVars = new DatadogEnvVars
            {
                DD_SERVICE = envVars.ServiceName,
                DD_ENV = envVars.Environment,
                DD_AGENT_HOST = envVars.AgentHost,
                DD_TRACE_AGENT_PORT = envVars.AgentPort,
                DD_LOGS_INJECTION = envVars.LogsInjectionEnabled,
                DD_TRACE_ENABLED = envVars.TraceEnabled,
                DD_VERSION = envVars.Version,
                ASPNETCORE_ENVIRONMENT = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            },
            
            // Expected values (what tracer should be using)
            ExpectedAgentUri = expectedAgentUri.ToString(),
            ExpectedEnvironment = effectiveEnv
        };
    }

    /// <summary>
    /// Attempts to reach the Datadog agent and returns connectivity status.
    /// </summary>
    public static async Task<AgentConnectivityResult> CheckAgentConnectivityAsync(
        HttpClient httpClient, 
        string? agentUri = null)
    {
        var uri = agentUri ?? Tracer.Instance.Settings.AgentUri?.ToString();
        
        if (string.IsNullOrEmpty(uri))
        {
            return new AgentConnectivityResult
            {
                Status = "no_agent_uri",
                Message = "Agent URI is not configured"
            };
        }

        try
        {
            var infoUrl = $"{uri.TrimEnd('/')}/info";
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await httpClient.GetAsync(infoUrl, cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);
                return new AgentConnectivityResult
                {
                    Status = "reachable",
                    StatusCode = (int)response.StatusCode,
                    AgentInfo = content
                };
            }
            
            return new AgentConnectivityResult
            {
                Status = $"http_{(int)response.StatusCode}",
                StatusCode = (int)response.StatusCode,
                Message = $"Agent returned HTTP {(int)response.StatusCode}"
            };
        }
        catch (HttpRequestException ex)
        {
            return new AgentConnectivityResult
            {
                Status = "unreachable",
                Message = $"Connection failed: {ex.Message}"
            };
        }
        catch (TaskCanceledException)
        {
            return new AgentConnectivityResult
            {
                Status = "timeout",
                Message = "Connection timed out after 5 seconds"
            };
        }
        catch (Exception ex)
        {
            return new AgentConnectivityResult
            {
                Status = "error",
                Message = $"Unexpected error: {ex.Message}"
            };
        }
    }

    private static DatadogEnvVarValues GetEnvironmentVariables()
    {
        return new DatadogEnvVarValues
        {
            ServiceName = Environment.GetEnvironmentVariable("DD_SERVICE"),
            Environment = Environment.GetEnvironmentVariable("DD_ENV"),
            AgentHost = Environment.GetEnvironmentVariable("DD_AGENT_HOST"),
            AgentPort = Environment.GetEnvironmentVariable("DD_TRACE_AGENT_PORT"),
            LogsInjectionEnabled = Environment.GetEnvironmentVariable("DD_LOGS_INJECTION"),
            TraceEnabled = Environment.GetEnvironmentVariable("DD_TRACE_ENABLED"),
            Version = Environment.GetEnvironmentVariable("DD_VERSION")
        };
    }

    private static Uri BuildAgentUri(string? host, string? port)
    {
        var effectiveHost = string.IsNullOrEmpty(host) ? "127.0.0.1" : host;
        var effectivePort = string.IsNullOrEmpty(port) ? "8126" : port;
        return new Uri($"http://{effectiveHost}:{effectivePort}/");
    }

    private static string ResolveEnvironment(string? ddEnv)
    {
        // Priority: DD_ENV > ASPNETCORE_ENVIRONMENT > "production"
        if (!string.IsNullOrEmpty(ddEnv))
            return ddEnv;
        
        var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (!string.IsNullOrEmpty(aspnetEnv))
            return aspnetEnv.ToLowerInvariant();
        
        return "production";
    }

    private record DatadogEnvVarValues
    {
        public string? ServiceName { get; init; }
        public string? Environment { get; init; }
        public string? AgentHost { get; init; }
        public string? AgentPort { get; init; }
        public string? LogsInjectionEnabled { get; init; }
        public string? TraceEnabled { get; init; }
        public string? Version { get; init; }
    }
}

/// <summary>
/// Diagnostic information about Datadog tracer configuration.
/// </summary>
public record DatadogDiagnostics
{
    // Actual tracer settings
    public required string ServiceName { get; init; }
    public required string Environment { get; init; }
    public required string ServiceVersion { get; init; }
    public required string AgentUri { get; init; }
    public bool LogsInjectionEnabled { get; init; }
    public bool TracerEnabled { get; init; }
    
    // Environment variables (for debugging)
    public DatadogEnvVars? EnvVars { get; init; }
    
    // Expected values based on env vars
    public string? ExpectedAgentUri { get; init; }
    public string? ExpectedEnvironment { get; init; }
}

/// <summary>
/// Raw environment variable values for diagnostic purposes.
/// </summary>
public record DatadogEnvVars
{
    public string? DD_SERVICE { get; init; }
    public string? DD_ENV { get; init; }
    public string? DD_AGENT_HOST { get; init; }
    public string? DD_TRACE_AGENT_PORT { get; init; }
    public string? DD_LOGS_INJECTION { get; init; }
    public string? DD_TRACE_ENABLED { get; init; }
    public string? DD_VERSION { get; init; }
    public string? ASPNETCORE_ENVIRONMENT { get; init; }
}

/// <summary>
/// Result of attempting to connect to the Datadog agent.
/// </summary>
public record AgentConnectivityResult
{
    public required string Status { get; init; }
    public int? StatusCode { get; init; }
    public string? Message { get; init; }
    public string? AgentInfo { get; init; }
}
