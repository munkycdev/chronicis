using Datadog.Trace;
using Datadog.Trace.Configuration;

namespace Chronicis.Api.Infrastructure;

/// <summary>
/// Helper for configuring Datadog APM tracing.
/// Configuration is primarily driven by DD_* environment variables.
/// </summary>
public static class DatadogConfiguration
{
    /// <summary>
    /// Configures and starts Datadog tracing.
    /// Safe to call even if agent is not available (tracer will buffer/drop spans).
    /// </summary>
    public static void ConfigureTracing(Serilog.ILogger logger)
    {
        try
        {
            var settings = TracerSettings.FromDefaultSources();
            
            // Service name (DD_SERVICE env var, or default)
            settings.ServiceName = Environment.GetEnvironmentVariable("DD_SERVICE") 
                ?? "chronicis-api";
            
            // Environment (DD_ENV env var, or default)
            settings.Environment = Environment.GetEnvironmentVariable("DD_ENV") 
                ?? "development";
            
            // Version (DD_VERSION env var, or assembly version)
            var version = Environment.GetEnvironmentVariable("DD_VERSION");
            if (string.IsNullOrEmpty(version))
            {
                version = typeof(DatadogConfiguration).Assembly
                    .GetName().Version?.ToString() ?? "1.0.0";
            }
            settings.ServiceVersion = version;
            
            // Agent endpoint (DD_AGENT_HOST and DD_TRACE_AGENT_PORT env vars)
            var agentHost = Environment.GetEnvironmentVariable("DD_AGENT_HOST") ?? "datadog-agent";
            var agentPort = Environment.GetEnvironmentVariable("DD_TRACE_AGENT_PORT") ?? "8126";
            var agentUri = new Uri($"http://{agentHost}:{agentPort}");
            settings.AgentUri = agentUri;
            
            // Enable log correlation (injects trace IDs into logs)
            settings.LogsInjectionEnabled = true;
            
            // Set the global tracer
            Tracer.Configure(settings);
            
            logger.Information(
                "Datadog APM configured: Service={Service}, Env={Env}, Version={Version}, Agent={AgentUri}",
                settings.ServiceName,
                settings.Environment,
                settings.ServiceVersion,
                agentUri);
        }
        catch (Exception ex)
        {
            // Don't fail startup if tracing configuration fails
            logger.Warning(ex, "Failed to configure Datadog tracing. Traces may not be collected.");
        }
    }
    
    /// <summary>
    /// Gets current tracer configuration for diagnostics.
    /// </summary>
    public static DatadogDiagnostics GetDiagnostics()
    {
        var tracer = Tracer.Instance;
        var settings = tracer.Settings;
        
        return new DatadogDiagnostics
        {
            ServiceName = settings.ServiceName ?? "chronicis-api",
            Environment = settings.Environment ?? "development",
            ServiceVersion = settings.ServiceVersion ?? "0.0.0",
            AgentUri = settings.AgentUri?.ToString() ?? "datadog-agent",
            LogsInjectionEnabled = settings.LogsInjectionEnabled,
            TracerEnabled = settings.TraceEnabled
        };
    }
}

/// <summary>
/// Diagnostic information about Datadog tracer configuration.
/// </summary>
public record DatadogDiagnostics
{
    public required string ServiceName { get; init; }
    public required string Environment { get; init; }
    public required string ServiceVersion { get; init; }
    public required string AgentUri { get; init; }
    public bool LogsInjectionEnabled { get; init; }
    public bool TracerEnabled { get; init; }
}
