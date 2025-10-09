# PowerShell script to stop Elasticsearch and Kibana

Write-Host "Stopping Elasticsearch and Kibana..." -ForegroundColor Yellow

docker-compose down

Write-Host "`n✓ Services stopped successfully" -ForegroundColor Green
Write-Host ""
Write-Host "To remove data volumes as well, run: " -NoNewline
Write-Host "docker-compose down -v" -ForegroundColor Yellow
Write-Host ""

