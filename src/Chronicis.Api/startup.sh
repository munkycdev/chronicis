#!/bin/bash

# Chronicis API Startup Script with DataDog APM
# This script downloads and configures the DataDog .NET tracer before starting the application

echo "Starting Chronicis API with DataDog APM instrumentation..."

# Create directories for Datadog tracer and .NET logs
mkdir -p /datadog/tracer
mkdir -p /home/LogFiles/dotnet

# Download the Datadog tracer (using v3.9.0 to match NuGet package version, or use latest stable)
TRACER_VERSION="3.9.0"
TRACER_URL="https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm-${TRACER_VERSION}.tar.gz"

echo "Downloading DataDog .NET tracer v${TRACER_VERSION}..."
wget -q -O /datadog/tracer/datadog-dotnet-apm.tar.gz "$TRACER_URL"

# Extract the tracer
echo "Extracting tracer..."
pushd /datadog/tracer > /dev/null
tar -zxf datadog-dotnet-apm.tar.gz
popd > /dev/null

# Set required environment variables for .NET Core CLR Profiler
export CORECLR_ENABLE_PROFILING=1
export CORECLR_PROFILER="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
export CORECLR_PROFILER_PATH="/datadog/tracer/Datadog.Trace.ClrProfiler.Native.so"
export DD_DOTNET_TRACER_HOME="/datadog/tracer"

# Log configuration for debugging
echo "DataDog tracer configured:"
echo "  CORECLR_ENABLE_PROFILING=$CORECLR_ENABLE_PROFILING"
echo "  CORECLR_PROFILER=$CORECLR_PROFILER"
echo "  CORECLR_PROFILER_PATH=$CORECLR_PROFILER_PATH"
echo "  DD_DOTNET_TRACER_HOME=$DD_DOTNET_TRACER_HOME"
echo "  DD_SERVICE=$DD_SERVICE"
echo "  DD_ENV=$DD_ENV"
echo "  DD_VERSION=$DD_VERSION"
echo "  DD_SITE=$DD_SITE"

# Start the application
echo "Starting Chronicis.Api..."
dotnet /home/site/wwwroot/Chronicis.Api.dll
