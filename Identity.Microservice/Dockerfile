# ============================================================
# UMS IAM — Identity.Api Dockerfile
# Multi-stage build for .NET 8 (Production-Ready)
# Author: Ahmed Almahallawi
# ============================================================

# ------------------------------------------------------------
# STAGE 1: BASE — Runtime only (صغير، آمن)
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# إنشاء مستخدم غير جذري (Non-root user) للأمان
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# ------------------------------------------------------------
# STAGE 2: BUILD — SDK كامل للبناء
# ------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# نسخ ملفات المشروع أولاً (للاستفادة من الـ Cache)
COPY ["Identity.Api.csproj", "./"]
RUN dotnet restore "./Identity.Api.csproj"

# نسخ باقي الملفات
COPY . .
WORKDIR "/src/."

# بناء المشروع
RUN dotnet build "./Identity.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/build

# ------------------------------------------------------------
# STAGE 3: PUBLISH — نشر التطبيق
# ------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Identity.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# ------------------------------------------------------------
# STAGE 4: FINAL — الصورة النهائية (خفيفة وآمنة)
# ------------------------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check للتأكد من أن التطبيق يعمل
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Identity.Api.dll"]
