# ============================================================
# UMS IAM — Identity.Api Dockerfile
# Multi-stage build for .NET 10 (Production-Ready)
# Author: Ahmed Almahallawi
# ============================================================

# ------------------------------------------------------------
# STAGE 1: BASE — Runtime only (small, secure)
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# .NET 8+ images already include a non-root user 'app' (UID 1654)
# Do not use useradd/adduser - it fails on minimal/chiseled images
# Using the built-in user is the official best practice
USER app

# ------------------------------------------------------------
# STAGE 2: BUILD — Full SDK for building
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
# STAGE 3: PUBLISH — Publish the application
# ------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Identity.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ------------------------------------------------------------
# STAGE 4: FINAL — Final lightweight and secure image
# ------------------------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check to verify the app is running
# Uses wget because curl is not available in minimal images
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Identity.Api.dll"]