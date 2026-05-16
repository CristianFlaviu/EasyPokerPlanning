# syntax=docker/dockerfile:1.7
# Multi-stage build for PokerPlanning.Api (.NET 10)
# Build context: backend/

ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY PokerPlanning.slnx ./
COPY src/PokerPlanning.Domain/PokerPlanning.Domain.csproj            src/PokerPlanning.Domain/
COPY src/PokerPlanning.Application/PokerPlanning.Application.csproj  src/PokerPlanning.Application/
COPY src/PokerPlanning.Infrastructure/PokerPlanning.Infrastructure.csproj src/PokerPlanning.Infrastructure/
COPY src/PokerPlanning.ServiceDefaults/PokerPlanning.ServiceDefaults.csproj src/PokerPlanning.ServiceDefaults/
COPY src/PokerPlanning.Api/PokerPlanning.Api.csproj                  src/PokerPlanning.Api/

RUN dotnet restore src/PokerPlanning.Api/PokerPlanning.Api.csproj

# Copy sources and publish
COPY src/ src/
RUN dotnet publish src/PokerPlanning.Api/PokerPlanning.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish ./

# Non-root user (image already has app user uid 1654)
USER app

ENTRYPOINT ["dotnet", "PokerPlanning.Api.dll"]
