# P6 — one container per service, multi-stage.
#
# Build context is the REPOSITORY ROOT (flyio/api.fly.toml declares
# `context = ".."`), because the COPY lines below reference paths from there.
# The root .dockerignore is exclusion-based, so this context carries the solution
# and nothing else — not web/, not tests/, not docs/.

# ---- build ------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Project files first, then restore, then source. A code change must not
# invalidate the restore layer — that ordering is most of the build time.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/ArchitectureStandardsInitExample.ServiceDefaults/ArchitectureStandardsInitExample.ServiceDefaults.csproj src/ArchitectureStandardsInitExample.ServiceDefaults/
COPY src/ArchitectureStandardsInitExample.Contracts/ArchitectureStandardsInitExample.Contracts.csproj src/ArchitectureStandardsInitExample.Contracts/
COPY src/ArchitectureStandardsInitExample.Api/ArchitectureStandardsInitExample.Api.csproj src/ArchitectureStandardsInitExample.Api/
RUN dotnet restore src/ArchitectureStandardsInitExample.Api/ArchitectureStandardsInitExample.Api.csproj

COPY src/ src/
RUN dotnet publish src/ArchitectureStandardsInitExample.Api/ArchitectureStandardsInitExample.Api.csproj \
        -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- final ------------------------------------------------------------------
# The runtime image major version equals the TFM major version. Roll-forward does
# not cross a major, and getting this wrong is a startup failure, not a warning.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS final
WORKDIR /app

LABEL org.opencontainers.image.source="https://github.com/konradcinkusz/architecture-standards-init-example"
LABEL org.opencontainers.image.description="The one service of architecture-standards-init-example. Owns apidb."
LABEL org.opencontainers.image.licenses="MIT"

# Bind all interfaces on the port fly.toml's internal_port names. A container
# that binds localhost is unreachable from the Fly proxy (FLY-IO §2).
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

COPY --from=build /app/publish .

# Non-root. APP_UID is defined by the official .NET images (uid 1654).
USER $APP_UID

ENTRYPOINT ["dotnet", "ArchitectureStandardsInitExample.Api.dll"]
