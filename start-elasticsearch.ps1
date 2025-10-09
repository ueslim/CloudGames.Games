# PowerShell script to start Elasticsearch and Kibana for local development

Write-Host "Starting Elasticsearch and Kibana..." -ForegroundColor Green

docker-compose up -d

Write-Host "`nWaiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "`nChecking Elasticsearch health..." -ForegroundColor Yellow
$maxAttempts = 30
$attempt = 0
$healthy = $false

while ($attempt -lt $maxAttempts -and -not $healthy) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:9200/_cluster/health" -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $healthy = $true
            Write-Host "✓ Elasticsearch is healthy!" -ForegroundColor Green
        }
    }
    catch {
        $attempt++
        Write-Host "Attempt $attempt/$maxAttempts - Waiting for Elasticsearch..." -ForegroundColor Gray
        Start-Sleep -Seconds 2
    }
}

if (-not $healthy) {
    Write-Host "✗ Elasticsearch failed to start properly" -ForegroundColor Red
    Write-Host "Check logs with: docker-compose logs elasticsearch" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "Services are ready!" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host ""
Write-Host "Elasticsearch: " -NoNewline
Write-Host "http://localhost:9200" -ForegroundColor Cyan
Write-Host "Kibana:        " -NoNewline
Write-Host "http://localhost:5601" -ForegroundColor Cyan
Write-Host ""
Write-Host "To stop services, run: " -NoNewline
Write-Host ".\stop-elasticsearch.ps1" -ForegroundColor Yellow
Write-Host ""

