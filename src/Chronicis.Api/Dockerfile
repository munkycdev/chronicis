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
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Expose port 8080 (Azure App Service expects this)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Start the application
ENTRYPOINT ["dotnet", "Chronicis.Api.dll"]
