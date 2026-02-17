# Chronicis Observability Guide

This document describes how to configure and validate observability for the Chronicis API.

## DataDog APM

Chronicis uses DataDog APM for distributed tracing with an in-image agent configuration. The DataDog .NET tracer is embedded in the application container and sends traces directly to the Datadog cloud.

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│ Azure Container App (ca-chronicis-api)                  │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │ chronicis-api (.NET 9)                          │   │
│  │ - ASP.NET Core Web API                          │   │
│  │ - DataDog .NET Tracer (embedded)                │   │
│  │ - Serilog with DataDog sink                     │   │
│  └─────────────────────────────────────────────────┘   │
│                                                         │
└───────────────────────────────────────┼─────────────────┘
                                        │
                                        │ HTTPS (direct)
                                        ▼
                              ┌─────────────────┐
                              │ Datadog Cloud   │
                              │ - APM Traces    │
                              │ - Logs          │
                              │ - Metrics       │
                              └─────────────────┘
```

### Required Environment Variables

Configure these in Azure Container Apps → Configuration → Environment Variables:

| Variable | Value | Description |
|----------|-------|-------------|
| `DD_API_KEY` | `<api-key>` | DataDog API key (from Key Vault secret) |
| `DD_SITE` | `datadoghq.com` | DataDog site (US datacenter) |
| `DD_SERVICE` | `chronicis-api` | Service name in Datadog APM |
| `DD_ENV` | `production` | Environment tag (production, staging, development) |
| `DD_VERSION` | `<build-number>` | Version tag for deployments |
| `DD_LOGS_INJECTION` | `true` | Inject trace IDs into logs |
| `DD_RUNTIME_METRICS_ENABLED` | `true` | Enable .NET runtime metrics |
| `DD_TRACE_AGENT_URL` | (not set) | When empty, tracer sends directly to cloud |

### Container Configuration

The DataDog .NET tracer is installed in the container image during build. No separate agent container is required.

**Dockerfile snippet:**
```dockerfile
# Install DataDog tracer
RUN mkdir -p /opt/datadog
RUN curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v2.x.x/datadog-dotnet-apm-2.x.x.tar.gz
RUN tar -xzf datadog-dotnet-apm-2.x.x.tar.gz -C /opt/datadog

# Set environment variables
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
```

### Validation

#### 1. Health Check

```bash
curl https://api.chronicis.app/health
# Expected: {"status":"healthy","timestamp":"..."}
```

#### 2. DataDog Diagnostic Endpoint

```bash
curl https://api.chronicis.app/diag/datadog
```

Expected response:
```json
{
  "timestamp": "2026-01-27T...",
  "tracer": {
    "serviceName": "chronicis-api",
    "environment": "production",
    "serviceVersion": "1.0.0",
    "agentUri": null,
    "logsInjectionEnabled": true,
    "tracerEnabled": true,
    "runtimeMetricsEnabled": true
  },
  "agent": {
    "status": "direct-to-cloud",
    "info": "Traces sent directly to Datadog cloud"
  }
}
```

#### 3. DataDog UI

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

When running locally without DataDog configuration:
- Tracing is configured but traces are dropped (no API key)
- The `/diag/datadog` endpoint will show tracer configuration
- This is expected and doesn't affect application functionality

To test with DataDog locally:

```powershell
# Set environment variables for local dev
$env:DD_API_KEY = "<your-api-key>"
$env:DD_SITE = "datadoghq.com"
$env:DD_SERVICE = "chronicis-api"
$env:DD_ENV = "development"
$env:DD_LOGS_INJECTION = "true"
$env:DD_RUNTIME_METRICS_ENABLED = "true"

# Run the API
cd src\Chronicis.Api
dotnet run
```

**Note:** For local development, you may prefer to omit `DD_API_KEY` to avoid sending development traces to production Datadog.

### Troubleshooting

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| No traces in Datadog | Missing `DD_API_KEY` | Verify API key in Container App environment variables |
| Traces appear but no service name | `DD_SERVICE` not set | Add to Container App configuration |
| Missing trace IDs in logs | `DD_LOGS_INJECTION` not enabled | Set to `true` |
| High memory usage | Too many metrics enabled | Review `DD_RUNTIME_METRICS_ENABLED` setting |
| Traces delayed | Network latency | Check connectivity to Datadog cloud endpoint |

### Container App Configuration

Ensure the following in your Container App configuration:

**Environment Variables:**
- All `DD_*` variables set as described above
- `DD_API_KEY` sourced from Key Vault secret reference

**Secrets:**
```bash
az containerapp secret set \
  --name ca-chronicis-api \
  --resource-group rg-chronicis \
  --secrets dd-api-key=keyvaultref:<key-vault-secret-uri>,identityref:<managed-identity-id>
```

**Environment Variable Reference:**
```bash
az containerapp update \
  --name ca-chronicis-api \
  --resource-group rg-chronicis \
  --set-env-vars "DD_API_KEY=secretref:dd-api-key"
```

### Performance Impact

The DataDog tracer has minimal performance overhead:
- **CPU:** < 1% in typical workloads
- **Memory:** ~50-100 MB additional memory usage
- **Network:** Batched trace uploads, minimal bandwidth

### References

- [DataDog .NET Tracing](https://docs.datadoghq.com/tracing/trace_collection/dd_libraries/dotnet-core/)
- [Azure Container Apps Environment Variables](https://learn.microsoft.com/en-us/azure/container-apps/environment-variables)
- [Serilog + Datadog](https://docs.datadoghq.com/logs/log_collection/csharp/)
