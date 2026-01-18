# Chronicis API Dockerfile
# Multi-stage build for optimized container size

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY ["src/Chronicis.Api/Chronicis.Api.csproj", "Chronicis.Api/"]
COPY ["src/Chronicis.Shared/Chronicis.Shared.csproj", "Chronicis.Shared/"]

# Restore dependencies
RUN dotnet restore "Chronicis.Api/Chronicis.Api.csproj"

# Copy all source code
COPY src/Chronicis.Api/ Chronicis.Api/
COPY src/Chronicis.Shared/ Chronicis.Shared/

# Build and publish
WORKDIR /src/Chronicis.Api
RUN dotnet publish -c Release -o /app/publish -p:PublishReadyToRun=true

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# ---- Datadog APM: install the .NET tracer into the runtime image ----
ARG DD_TRACE_VERSION=3.31.0

RUN apt-get update \
  && apt-get install -y --no-install-recommends ca-certificates curl tar \
  && rm -rf /var/lib/apt/lists/*

RUN set -eux; \
  mkdir -p /opt/datadog; \
  curl -fL --retry 5 --retry-delay 2 --retry-all-errors \
    -o /tmp/datadog-dotnet-apm.tar.gz \
    "https://github.com/DataDog/dd-trace-dotnet/releases/download/v3.31.0/dd-trace-linux-x64.tar.gz"; \
  tar -xzf /tmp/datadog-dotnet-apm.tar.gz -C /opt/datadog; \
  rm /tmp/datadog-dotnet-apm.tar.gz; \
  test -f /opt/datadog/Datadog.Trace.ClrProfiler.Native.so

ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}" \
    CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so \
    DD_DOTNET_TRACER_HOME=/opt/datadog
# --------------------------------------------------------------------


# Copy published application
COPY --from=build /app/publish .

# Expose port 8080 (Azure App Service expects this)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Start the application
ENTRYPOINT ["dotnet", "Chronicis.Api.dll"]
