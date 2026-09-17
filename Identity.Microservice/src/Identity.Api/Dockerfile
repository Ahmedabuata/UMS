# ============================================================
# UMS IAM — Identity.Api Dockerfile
# Multi-stage build for .NET 8 (Production-Ready)
# Author: Ahmed Almahallawi
# ============================================================

# ------------------------------------------------------------
# STAGE 1: BASE — Runtime only (small, secure)
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# .NET 8+ images include a built-in non-root user 'app' (UID 1654)
# Do NOT use adduser/useradd — they fail on minimal images
USER app

# ------------------------------------------------------------
# STAGE 2: BUILD — Full SDK for building
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj first to leverage Docker layer cache
COPY ["Identity.Api.csproj", "./"]
RUN dotnet restore "./Identity.Api.csproj"

# Copy remaining files
COPY . .
WORKDIR "/src/."

# Build the project
RUN dotnet build "./Identity.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

# ------------------------------------------------------------
# STAGE 3: PUBLISH
# ------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Identity.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ------------------------------------------------------------
# STAGE 4: FINAL — Lightweight and secure image
# ------------------------------------------------------------
FROM base AS final
WORKDIR /app

# Ensure app owns the files (using built-in user)
COPY --from=publish --chown=app:app /app/publish .

# Health check — wget is available in Debian-based images
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Identity.Api.dll"]