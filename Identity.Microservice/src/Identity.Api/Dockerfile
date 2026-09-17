# ============================================================
# UMS IAM — Identity.Api Dockerfile
# Multi-stage build for .NET 8
# ============================================================

# ------------------------------------------------------------
# Runtime image
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Built-in non-root user from the official .NET image
USER app


# ------------------------------------------------------------
# Build image
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["Identity.Api.csproj", "./"]

RUN dotnet restore "Identity.Api.csproj"

COPY . .

RUN dotnet build "Identity.Api.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --no-restore \
    --output /app/build


# ------------------------------------------------------------
# Publish image
# ------------------------------------------------------------
FROM build AS publish

ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "Identity.Api.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false


# ------------------------------------------------------------
# Final runtime image
# ------------------------------------------------------------
FROM base AS final

WORKDIR /app

COPY --from=publish --chown=app:app /app/publish .

ENTRYPOINT ["dotnet", "Identity.Api.dll"]
