# build-and-run.ps1
# Builds and starts all Docker containers (Angular is built inside Docker)

Write-Host "Building Docker images..." -ForegroundColor Cyan
docker compose -f infrastructure/docker-compose.yml build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker build failed! Retrying with --no-cache..." -ForegroundColor Yellow
    docker compose -f infrastructure/docker-compose.yml build --no-cache
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Docker build failed!" -ForegroundColor Red
        exit 1
    }
}
Write-Host "Docker build complete." -ForegroundColor Green

Write-Host ""
Write-Host "Starting all services..." -ForegroundColor Cyan
docker compose -f infrastructure/docker-compose.yml up -d

Write-Host ""
Write-Host "All services starting up. Access points:" -ForegroundColor Green
Write-Host "  Frontend    -> http://localhost:4200"
Write-Host "  API Gateway -> http://localhost:5000"
Write-Host "  RabbitMQ    -> http://localhost:15672  (grocery/grocery123)"
Write-Host ""
Write-Host "To view logs:  docker compose -f infrastructure/docker-compose.yml logs -f"
Write-Host "To stop:       docker compose -f infrastructure/docker-compose.yml down"
