using System.Diagnostics.CodeAnalysis;
using System.Net;
using Chronicis.Api.Infrastructure;
using Chronicis.Api.Tests.TestDoubles;
using Datadog.Trace;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class DatadogDiagnosticsTests
{
    [Fact]
    public void ReadEnvironmentVariables_ReturnsRawValues()
    {
        using var _ = new EnvironmentScope(new Dictionary<string, string?>
        {
            ["DD_SERVICE"] = "chronicis-api",
            ["DD_ENV"] = "test",
            ["DD_VERSION"] = "1.2.3",
            ["DD_AGENT_HOST"] = "localhost",
            ["DD_TRACE_AGENT_PORT"] = "8126",
            ["DD_LOGS_INJECTION"] = "true",
            ["DD_TRACE_ENABLED"] = "true",
            ["DD_TRACE_AGENT_URL"] = "http://localhost:8126",
            ["ASPNETCORE_ENVIRONMENT"] = "Development"
        });

        var result = DatadogDiagnostics.ReadEnvironmentVariables();

        Assert.Equal("chronicis-api", result.DD_SERVICE);
        Assert.Equal("test", result.DD_ENV);
        Assert.Equal("1.2.3", result.DD_VERSION);
        Assert.Equal("localhost", result.DD_AGENT_HOST);
        Assert.Equal("8126", result.DD_TRACE_AGENT_PORT);
        Assert.Equal("true", result.DD_LOGS_INJECTION);
        Assert.Equal("true", result.DD_TRACE_ENABLED);
        Assert.Equal("http://localhost:8126", result.DD_TRACE_AGENT_URL);
        Assert.Equal("Development", result.ASPNETCORE_ENVIRONMENT);
    }

    [Fact]
    public void ReadTracerState_ReturnsSnapshotFromTracerSettings()
    {
        var settings = Tracer.Instance.Settings;

        var result = DatadogDiagnostics.ReadTracerState();

        Assert.Equal(settings.ServiceName, result.ServiceName);
        Assert.Equal(settings.Environment, result.Environment);
        Assert.Equal(settings.ServiceVersion, result.ServiceVersion);
        Assert.Equal(settings.AgentUri?.ToString(), result.AgentUri);
        Assert.Equal(settings.LogsInjectionEnabled, result.LogsInjectionEnabled);
        Assert.Equal(settings.TraceEnabled, result.TraceEnabled);
    }

    [Fact]
    public void UriToStringOrNull_ReturnsNull_WhenUriIsNull()
    {
        var result = DatadogDiagnostics.UriToStringOrNull(null);

        Assert.Null(result);
    }

    [Fact]
    public void UriToStringOrNull_ReturnsString_WhenUriIsPresent()
    {
        var result = DatadogDiagnostics.UriToStringOrNull(new Uri("http://localhost:8126"));

        Assert.Equal("http://localhost:8126/", result);
    }

    [Fact]
    public void LogTracerState_DoesNotThrow_WhenReadersSucceed()
    {
        var logger = Substitute.For<Serilog.ILogger>();

        DatadogDiagnostics.LogTracerState(
            logger,
            () => new DatadogEnvVars(),
            () => new DatadogTracerState
            {
                TraceEnabled = true
            });

        logger.DidNotReceive().Warning(
            Arg.Any<Exception>(),
            "Failed to read Datadog tracer state");
    }

    [Fact]
    public void LogTracerState_LogsWarning_WhenReaderThrows()
    {
        var logger = Substitute.For<Serilog.ILogger>();
        var thrown = new InvalidOperationException("boom");

        DatadogDiagnostics.LogTracerState(
            logger,
            () => throw thrown,
            () => new DatadogTracerState());

        logger.Received(1).Warning(
            Arg.Is<Exception>(ex => ReferenceEquals(ex, thrown)),
            "Failed to read Datadog tracer state");
    }

    [Fact]
    public void PublicLogTracerState_SmokeTest_DoesNotThrow()
    {
        var logger = Substitute.For<Serilog.ILogger>();

        DatadogDiagnostics.LogTracerState(logger);

        logger.DidNotReceive().Warning(
            Arg.Any<Exception>(),
            "Failed to read Datadog tracer state");
    }

    [Fact]
    public async Task CheckAgentConnectivity_NoUrl_ReturnsSkipped()
    {
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);

        var result = await DatadogDiagnostics.CheckAgentConnectivityAsync(client, "");

        Assert.Equal("(not provided)", result.Url);
        Assert.Equal("skipped", result.Status);
        Assert.Equal("No URL provided", result.Message);
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public async Task CheckAgentConnectivity_Success_ReturnsReachableAndBody()
    {
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"1.0\"}")
        });
        using var client = new HttpClient(handler);

        var result = await DatadogDiagnostics.CheckAgentConnectivityAsync(client, "http://dd-agent:8126/");

        Assert.Equal("http://dd-agent:8126/info", result.Url);
        Assert.Equal("reachable", result.Status);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("{\"version\":\"1.0\"}", result.AgentInfo);
    }

    [Fact]
    public async Task CheckAgentConnectivity_HttpFailure_ReturnsHttpError()
    {
        using var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var client = new HttpClient(handler);

        var result = await DatadogDiagnostics.CheckAgentConnectivityAsync(client, "http://dd-agent:8126");

        Assert.Equal("http://dd-agent:8126/info", result.Url);
        Assert.Equal("http_error", result.Status);
        Assert.Equal(503, result.StatusCode);
        Assert.Contains("HTTP 503", result.Message);
    }

    [Fact]
    public async Task CheckAgentConnectivity_HttpRequestException_ReturnsUnreachable()
    {
        using var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("no route to host"));
        using var client = new HttpClient(handler);

        var result = await DatadogDiagnostics.CheckAgentConnectivityAsync(client, "http://dd-agent:8126");

        Assert.Equal("http://dd-agent:8126/info", result.Url);
        Assert.Equal("unreachable", result.Status);
        Assert.Equal("no route to host", result.Message);
    }

    [Fact]
    public async Task CheckAgentConnectivity_TaskCanceled_ReturnsTimeout()
    {
        using var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException());
        using var client = new HttpClient(handler);

        var result = await DatadogDiagnostics.CheckAgentConnectivityAsync(client, "http://dd-agent:8126");

        Assert.Equal("http://dd-agent:8126/info", result.Url);
        Assert.Equal("timeout", result.Status);
        Assert.Equal("Connection timed out after 5 seconds", result.Message);
    }

    [Fact]
    public async Task CheckAgentConnectivity_UnexpectedException_ReturnsError()
    {
        using var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("unexpected"));
        using var client = new HttpClient(handler);

        var result = await DatadogDiagnostics.CheckAgentConnectivityAsync(client, "http://dd-agent:8126");

        Assert.Equal("http://dd-agent:8126/info", result.Url);
        Assert.Equal("error", result.Status);
        Assert.Equal("unexpected", result.Message);
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> _original = new();

        public EnvironmentScope(IReadOnlyDictionary<string, string?> values)
        {
            foreach (var (key, value) in values)
            {
                _original[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        public void Dispose()
        {
            foreach (var (key, value) in _original)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
