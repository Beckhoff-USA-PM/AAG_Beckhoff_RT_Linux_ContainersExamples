# Dockerfile for AdsRouterConsole
# This creates a lightweight ADS router that can connect containers to a host TwinCAT system

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["containers/router/src/AdsRouterConsole.csproj", "AdsRouterConsole/"]

ARG BUILD_CONFIGURATION=Release

# Restore packages from public NuGet (Beckhoff packages are available on nuget.org)
RUN dotnet restore "AdsRouterConsole/AdsRouterConsole.csproj"

# Copy source code
COPY containers/router/src/ ./AdsRouterConsole/
WORKDIR "/src/AdsRouterConsole"

# Build
RUN dotnet build "./AdsRouterConsole.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "./AdsRouterConsole.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 48898/tcp
EXPOSE 48899/udp

ENTRYPOINT ["dotnet", "AdsRouterConsole.dll"]
