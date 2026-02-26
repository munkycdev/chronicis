# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ARG APP_VERSION=0.0.0-local
ARG GIT_SHA=local

# Copy repo (context should be repo root)
COPY . .

# Restore + publish ONLY the API project (avoid Windows-only projects)
RUN dotnet restore ./src/Chronicis.Api/Chronicis.Api.csproj
RUN dotnet publish ./src/Chronicis.Api/Chronicis.Api.csproj \
    -c Release \
    -o /out \
    --no-restore \
    /p:InformationalVersion="${APP_VERSION}+${GIT_SHA}"

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Create Datadog log dir with correct permissions
RUN mkdir -p /var/log/datadog && chown -R appuser:appgroup /var/log/datadog

# Install Datadog .NET tracer (pin version)
ARG DD_TRACE_VERSION=3.36.0

RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends curl ca-certificates; \
    rm -rf /var/lib/apt/lists/*; \
    curl -fL --retry 5 --retry-delay 2 --retry-all-errors \
      -o /tmp/datadog-dotnet-apm.deb \
      "https://github.com/DataDog/dd-trace-dotnet/releases/download/v${DD_TRACE_VERSION}/datadog-dotnet-apm_${DD_TRACE_VERSION}_amd64.deb"; \
    dpkg -i /tmp/datadog-dotnet-apm.deb; \
    rm /tmp/datadog-dotnet-apm.deb; \
    test -f /opt/datadog/Datadog.Trace.ClrProfiler.Native.so

# Datadog init (in-container)
COPY --from=datadog/serverless-init:latest /datadog-init /app/datadog-init

# App files
COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:8080
# DD_VERSION is set here as a fallback; the CI workflow overrides it via
# `az containerapp update --set-env-vars` so the running container always
# reflects the real build version even across image rebuilds.
ARG APP_VERSION=0.0.0-local
ENV DD_VERSION=${APP_VERSION}
EXPOSE 8080

USER appuser

ENTRYPOINT ["/app/datadog-init"]
CMD ["dotnet", "Chronicis.Api.dll"]
