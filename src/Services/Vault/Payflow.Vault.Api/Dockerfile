# syntax=docker/dockerfile:1
# Build context is the repository root: docker build -f src/Services/Vault/Payflow.Vault.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/ src/

RUN dotnet publish src/Services/Vault/Payflow.Vault.Api/Payflow.Vault.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --uid 5678 --user-group appuser
COPY --from=build /app .
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=15s \
    CMD ["curl", "-f", "http://localhost:8080/health"]

ENTRYPOINT ["dotnet", "Payflow.Vault.Api.dll"]
