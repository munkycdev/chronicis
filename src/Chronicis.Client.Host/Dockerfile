# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install wasm-tools workload to enable IL trimming (tree shaking) for Blazor WASM.
# Done before COPY so this layer is cached independently of source changes.
RUN dotnet workload install wasm-tools

# Copy repo (context should be repo root)
COPY . .

# Restore + publish ONLY the Client Host project (avoid Windows-only projects)
RUN dotnet restore ./src/Chronicis.Client.Host/Chronicis.Client.Host.csproj
RUN dotnet publish ./src/Chronicis.Client.Host/Chronicis.Client.Host.csproj -c Release -o /out --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# App files
COPY --from=build /out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER appuser

CMD ["dotnet", "Chronicis.Client.Host.dll"]
