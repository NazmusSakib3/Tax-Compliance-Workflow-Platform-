param(
    [switch]$FullStack
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "Starting infrastructure services..."
docker compose -f "$root\docker-compose.yml" up -d postgres redis rabbitmq

if ($FullStack) {
    Write-Host "Starting API and frontend (full stack profile)..."
    docker compose -f "$root\docker-compose.yml" --profile full up -d --build api frontend
    Write-Host "API: http://localhost:8080/swagger"
    Write-Host "Frontend: http://localhost:4200"
} else {
    Write-Host "Infrastructure is ready."
    Write-Host "Run the API with: dotnet run --project `"$root\backend\src\TaxCompliance.Api\TaxCompliance.Api.csproj`""
    Write-Host "Run the frontend with: cd `"$root\frontend`" && npm start"
}

Write-Host "PostgreSQL: localhost:5432 | Redis: localhost:6379 | RabbitMQ UI: http://localhost:15672"
