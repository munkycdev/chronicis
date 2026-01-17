# Chronicis API Dockerfile
# Multi-stage build with DataDog APM tracer pre-installed

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
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime with DataDog tracer
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install required tools for DataDog tracer installation
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create directories for DataDog tracer and logs
RUN mkdir -p /datadog/tracer
RUN mkdir -p /home/LogFiles/dotnet

# Download and extract DataDog .NET tracer (v3.35.0 - latest as of Jan 2026)
ADD https://github.com/DataDog/dd-trace-dotnet/releases/download/v3.35.0/datadog-dotnet-apm-3.35.0.tar.gz /datadog/tracer/
RUN cd /datadog/tracer && tar -zxf datadog-dotnet-apm-3.35.0.tar.gz && rm datadog-dotnet-apm-3.35.0.tar.gz

# Set DataDog .NET Tracer environment variables
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/datadog/tracer/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/datadog/tracer

# Enable log injection and runtime metrics
ENV DD_LOGS_INJECTION=true
ENV DD_RUNTIME_METRICS_ENABLED=true

# Copy published application
COPY --from=build /app/publish .

# Expose port 8080 (Azure App Service expects this)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Start the application
ENTRYPOINT ["dotnet", "Chronicis.Api.dll"]
