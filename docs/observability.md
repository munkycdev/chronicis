# Chronicis Observability Guide

This document describes how to configure and validate observability for the Chronicis API.

## Datadog APM

Chronicis uses Datadog APM for distributed tracing. Traces are sent to a Datadog agent running as a sidecar container in Azure App Service.

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│ Azure App Service (Linux Container)                     │
│                                                         │
│  ┌─────────────────┐      ┌─────────────────────────┐  │
│  │ chronicis-api   │─────▶│ datadog-agent (sidecar) │  │
│  │ (.NET 9)        │:8126 │                         │  │
│  └─────────────────┘      └───────────┬─────────────┘  │
│                                       │                 │
└───────────────────────────────────────┼─────────────────┘
                                        │
                                        ▼
                              ┌─────────────────┐
                              │ Datadog Cloud   │
                              │ (APM, Logs)     │
                              └─────────────────┘
```

### Required Environment Variables

Configure these in Azure App Service → Configuration → Application Settings:

| Variable | Value | Description |
|----------|-------|-------------|
| `DD_SERVICE` | `chronicis-api` | Service name in Datadog APM |
| `DD_ENV` | `production` | Environment tag (production, staging, development) |
| `DD_VERSION` | `<build-number>` | Version tag for deployments |
| `DD_AGENT_HOST` | `datadog-agent` | Hostname of the sidecar container |
| `DD_TRACE_AGENT_PORT` | `8126` | Trace agent port (default) |
| `DD_LOGS_INJECTION` | `true` | Inject trace IDs into logs |
| `DD_RUNTIME_METRICS_ENABLED` | `true` | Enable .NET runtime metrics |

### Sidecar Configuration

The Datadog agent runs as a sidecar container. Configure in Azure App Service → Deployment Center or via ARM template:

```json
{
  "name": "datadog-agent",
  "image": "datadog/agent:latest",
  "resources": {
    "cpu": 0.5,
    "memory": "256Mi"
  },
  "environmentVariables": [
    { "name": "DD_API_KEY", "secretRef": "dd-api-key" },
    { "name": "DD_SITE", "value": "datadoghq.com" },
    { "name": "DD_APM_ENABLED", "value": "true" },
    { "name": "DD_APM_NON_LOCAL_TRAFFIC", "value": "true" }
  ]
}
```

### Validation

#### 1. Health Check

```bash
curl https://api.chronicis.app/health
# Expected: {"status":"healthy","timestamp":"..."}
```

#### 2. Datadog Diagnostic Endpoint

```bash
curl https://api.chronicis.app/diag/datadog
```

Expected response:
```json
{
  "timestamp": "2025-01-25T...",
  "tracer": {
    "serviceName": "chronicis-api",
    "environment": "production",
    "serviceVersion": "1.0.0",
    "agentUri": "http://datadog-agent:8126/",
    "logsInjectionEnabled": true,
    "tracerEnabled": true,
    "runtimeMetricsEnabled": true
  },
  "agent": {
    "status": "reachable",
    "info": "{...agent info...}"
  }
}
```

If `agent.status` is `"unreachable"`, check:
- Sidecar container is running
- `DD_AGENT_HOST` matches the sidecar container name
- Network connectivity between containers

#### 3. Datadog UI

1. Go to [Datadog APM → Services](https://app.datadoghq.com/apm/services)
2. Filter by `env:production`
3. Look for `chronicis-api` service
4. Verify traces are appearing for HTTP requests

### What Gets Traced

With the current configuration, the following are automatically traced:

- **Inbound HTTP requests** - All ASP.NET Core controller actions
- **Outbound HTTP calls** - Via `HttpClient` / `IHttpClientFactory`
- **Database queries** - Entity Framework Core / SQL Server (via automatic instrumentation)

### Log Correlation

With `DD_LOGS_INJECTION=true`, Serilog logs will include trace context:

```json
{
  "message": "Processing request",
  "dd.trace_id": "1234567890",
  "dd.span_id": "9876543210",
  "dd.service": "chronicis-api",
  "dd.env": "production",
  "dd.version": "1.0.0"
}
```

This allows correlating logs with traces in Datadog.

### Local Development

When running locally without a Datadog agent:
- Tracing is configured but traces are dropped (no agent to receive them)
- The `/diag/datadog` endpoint will show `agent.status: "unreachable"`
- This is expected and doesn't affect application functionality

To test with a local agent:

```powershell
# Run Datadog agent locally
docker run -d --name datadog-agent `
  -e DD_API_KEY=<your-api-key> `
  -e DD_APM_ENABLED=true `
  -e DD_APM_NON_LOCAL_TRAFFIC=true `
  -p 8126:8126 `
  datadog/agent:latest

# Set environment variables for local dev
$env:DD_AGENT_HOST = "localhost"
$env:DD_ENV = "development"
```

### Troubleshooting

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| No traces in Datadog | Agent not running or unreachable | Check `/diag/datadog` endpoint |
| `agent.status: "unreachable"` | Network/DNS issue | Verify `DD_AGENT_HOST` matches sidecar name |
| Traces appear but no service name | `DD_SERVICE` not set | Add to App Service configuration |
| Missing trace IDs in logs | `DD_LOGS_INJECTION` not enabled | Set to `true` |

### References

- [Datadog .NET Tracing](https://docs.datadoghq.com/tracing/trace_collection/dd_libraries/dotnet-core/)
- [Azure App Service Sidecars](https://learn.microsoft.com/en-us/azure/app-service/tutorial-custom-container-sidecar)
- [Serilog + Datadog](https://docs.datadoghq.com/logs/log_collection/csharp/)
