# Dockerfile for AdsClient
# This creates an ADS client that connects to the host TwinCAT system via the router

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["containers/AdsClient/src/AdsClient.csproj", "AdsClient/"]

ARG BUILD_CONFIGURATION=Release

# Restore packages from public NuGet (Beckhoff packages are available on nuget.org)
RUN dotnet restore "AdsClient/AdsClient.csproj"

# Copy source code
COPY containers/AdsClient/src/ ./AdsClient/
WORKDIR "/src/AdsClient"

# Build
RUN dotnet build "./AdsClient.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "./AdsClient.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AdsClient.dll"]
