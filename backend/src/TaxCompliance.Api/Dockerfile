FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Fail fast if Docker pulled a corrupted SDK layer
RUN dotnet --info
COPY backend ./backend
RUN dotnet restore backend/TaxCompliance.sln
RUN dotnet publish backend/src/TaxCompliance.Api/TaxCompliance.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/storage/uploads /app/data-protection-keys && chown -R app:app /app/storage /app/data-protection-keys
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER app
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "TaxCompliance.Api.dll"]

