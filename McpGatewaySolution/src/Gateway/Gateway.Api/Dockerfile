FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
RUN groupadd --system appuser && useradd --system --gid appuser --no-create-home appuser && mkdir -p /app/keys && chown -R appuser:appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Gateway/Gateway.Api/Gateway.Api.csproj", "src/Gateway/Gateway.Api/"]
COPY ["src/Gateway/Gateway.Core/Gateway.Core.csproj", "src/Gateway/Gateway.Core/"]
COPY ["src/ServiceDefaults/ServiceDefaults.csproj", "src/ServiceDefaults/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
RUN dotnet restore "./src/Gateway/Gateway.Api/Gateway.Api.csproj"
COPY . .
WORKDIR "/src/src/Gateway/Gateway.Api"
RUN dotnet build "./Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health/live || exit 1
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
