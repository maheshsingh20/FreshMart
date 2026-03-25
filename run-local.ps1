# run-local.ps1 - Start all microservices locally (no Docker for services)
# Prerequisites: .NET 8 SDK, SQL Server on localhost:1433, Redis on localhost:6379, RabbitMQ on localhost:5672
# Start infrastructure first: docker compose -f infrastructure/docker-compose.infra.yml up -d

$env:ASPNETCORE_ENVIRONMENT = "Development"

$services = @(
    @{ Name = "AuthService";         Path = "services/AuthService";         Port = 5001 },
    @{ Name = "ProductService";      Path = "services/ProductService";      Port = 5002 },
    @{ Name = "CartService";         Path = "services/CartService";         Port = 5003 },
    @{ Name = "OrderService";        Path = "services/OrderService";        Port = 5004 },
    @{ Name = "PaymentService";      Path = "services/PaymentService";      Port = 5005 },
    @{ Name = "DeliveryService";     Path = "services/DeliveryService";     Port = 5006 },
    @{ Name = "NotificationService"; Path = "services/NotificationService"; Port = 5007 },
    @{ Name = "ReviewService";       Path = "services/ReviewService";       Port = 5008 },
    @{ Name = "CouponService";       Path = "services/CouponService";       Port = 5009 },
    @{ Name = "AiService";           Path = "services/AiService";           Port = 5010 },
    @{ Name = "UserService";         Path = "services/UserService";         Port = 5011 },
    @{ Name = "SupportService";      Path = "services/SupportService";      Port = 5012 },
    @{ Name = "ApiGateway";          Path = "services/ApiGateway";          Port = 5000 }
)

$root = $PWD
$jobs = @()

foreach ($svc in $services) {
    Write-Host "Starting $($svc.Name) on :$($svc.Port)..." -ForegroundColor Cyan
    $job = Start-Job -Name $svc.Name -ScriptBlock {
        param($root, $path)
        Set-Location $root
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        dotnet run --project $path --no-launch-profile
    } -ArgumentList $root, $svc.Path
    $jobs += $job
    Start-Sleep -Milliseconds 500
}

Write-Host ""
Write-Host "All services starting. Port map:" -ForegroundColor Green
Write-Host "  ApiGateway          -> http://localhost:5000  (entry point)"
Write-Host "  AuthService         -> http://localhost:5001"
Write-Host "  ProductService      -> http://localhost:5002"
Write-Host "  CartService         -> http://localhost:5003"
Write-Host "  OrderService        -> http://localhost:5004"
Write-Host "  PaymentService      -> http://localhost:5005"
Write-Host "  DeliveryService     -> http://localhost:5006"
Write-Host "  NotificationService -> http://localhost:5007"
Write-Host "  ReviewService       -> http://localhost:5008"
Write-Host "  CouponService       -> http://localhost:5009"
Write-Host "  AiService           -> http://localhost:5010"
Write-Host "  UserService         -> http://localhost:5011"
Write-Host "  SupportService      -> http://localhost:5012"
Write-Host ""
Write-Host "Press Ctrl+C to stop all services." -ForegroundColor Yellow

try {
    while ($true) {
        foreach ($job in $jobs) {
            $output = Receive-Job -Job $job
            if ($output) { Write-Host "[$($job.Name)] $output" }
        }
        Start-Sleep -Seconds 2
    }
} finally {
    Write-Host "Stopping all services..." -ForegroundColor Red
    $jobs | Stop-Job
    $jobs | Remove-Job
}
