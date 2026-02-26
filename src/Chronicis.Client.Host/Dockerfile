# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ARG APP_VERSION=0.0.0-local
ARG GIT_SHA=local

# Install wasm-tools workload to enable IL trimming (tree shaking) for Blazor WASM.
# Done before COPY so this layer is cached independently of source changes.
RUN dotnet workload install wasm-tools

# Copy repo (context should be repo root)
COPY . .

# Stamp version.json so the running WASM app can read its own build version.
# BUILD_DATE is injected at image build time; the file is read by VersionService on startup.
RUN BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ") && \
    BUILD_NUMBER=$(echo "${APP_VERSION}" | awk -F. '{print $NF}') && \
    printf '{\n  "version": "%s",\n  "buildNumber": "%s",\n  "sha": "%s",\n  "buildDate": "%s"\n}\n' \
      "${APP_VERSION}" "${BUILD_NUMBER}" "${GIT_SHA}" "${BUILD_DATE}" \
      > src/Chronicis.Client/wwwroot/version.json && \
    echo "Stamped version.json:" && cat src/Chronicis.Client/wwwroot/version.json

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
# DD_VERSION is set here as a fallback; the CI workflow overrides it via
# `az containerapp update --set-env-vars` so the running container always
# reflects the real build version.
ARG APP_VERSION=0.0.0-local
ENV DD_VERSION=${APP_VERSION}
EXPOSE 8080

USER appuser

CMD ["dotnet", "Chronicis.Client.Host.dll"]
