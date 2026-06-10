# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY AIYouTubeFactory.sln .
COPY src/AIYouTubeFactory.API/AIYouTubeFactory.API.csproj src/AIYouTubeFactory.API/
COPY src/AIYouTubeFactory.Core/AIYouTubeFactory.Core.csproj src/AIYouTubeFactory.Core/
COPY src/AIYouTubeFactory.Infrastructure/AIYouTubeFactory.Infrastructure.csproj src/AIYouTubeFactory.Infrastructure/
COPY src/AIYouTubeFactory.Agents/AIYouTubeFactory.Agents.csproj src/AIYouTubeFactory.Agents/

RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish src/AIYouTubeFactory.API/AIYouTubeFactory.API.csproj \
    -c Release -o /app/publish --no-restore

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/api/content/health || exit 1

ENTRYPOINT ["dotnet", "AIYouTubeFactory.API.dll"]
