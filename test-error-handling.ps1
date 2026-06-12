# Test script to verify error handling works end-to-end
$ErrorActionPreference = "Continue"

Write-Host "Building the solution..." -ForegroundColor Cyan
cd "e:\Projects\OakIdeas.Aspire.DataExplorer"
dotnet build --configuration Debug --no-restore -q

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Starting the sample application..." -ForegroundColor Cyan
Write-Host "Note: The app will start in the background. Once you see 'Dashboard running', it's ready." -ForegroundColor Yellow

# Start the app in a background job
$job = Start-Job -ScriptBlock {
    cd "e:\Projects\OakIdeas.Aspire.DataExplorer\samples\OakIdeas.Aspire.DataExplorer.Sample.AppHost"
    dotnet run --configuration Debug --no-restore
}

# Wait for the dashboard to be ready
Write-Host "Waiting for the application to start (up to 60 seconds)..." -ForegroundColor Yellow
$timeout = 60
$elapsed = 0
$ready = $false

while ($elapsed -lt $timeout) {
    $output = Receive-Job -Job $job -Keep
    if ($output -match "Dashboard running" -or $output -match "Application started") {
        $ready = $true
        Write-Host "✓ Application is ready!" -ForegroundColor Green
        break
    }
    Start-Sleep -Seconds 2
    $elapsed += 2
    Write-Host "." -NoNewline
}

if (-not $ready) {
    Write-Host ""
    Write-Host "Warning: Application may still be starting. Showing recent output:" -ForegroundColor Yellow
    Receive-Job -Job $job -Keep | Select-Object -Last 20 | ForEach-Object { Write-Host $_ }
}

Write-Host ""
Write-Host "Application started. Press any key to open browser and test..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Start-Process "http://localhost:5000"
Start-Process "http://localhost:5001"

Write-Host ""
Write-Host "Browser opened. The Aspire dashboard is at http://localhost:5000" -ForegroundColor Green
Write-Host "The Data Explorer UI is at http://localhost:5001" -ForegroundColor Green
Write-Host ""
Write-Host "Instructions for testing:" -ForegroundColor Cyan
Write-Host "1. Go to the Data Explorer tab" -ForegroundColor White
Write-Host "2. Select the 'sampledb' database" -ForegroundColor White
Write-Host "3. Open the Query panel" -ForegroundColor White
Write-Host "4. Execute this query:" -ForegroundColor White
Write-Host "   SELECT * FROM NonExistentTable" -ForegroundColor Gray
Write-Host ""
Write-Host "Expected behavior:" -ForegroundColor Cyan
Write-Host "✓ App should NOT crash" -ForegroundColor White
Write-Host "✓ Error should display in Query panel with:" -ForegroundColor White
Write-Host "  - Category, Operation, Target, Code" -ForegroundColor Gray
Write-Host "  - Message (NEW)" -ForegroundColor Green
Write-Host "  - Recovery Suggestion (NEW)" -ForegroundColor Green
Write-Host ""
Write-Host "✓ Server logs should show exception details" -ForegroundColor White
Write-Host ""
Write-Host "Press CTRL+C when done testing to stop the server..." -ForegroundColor Yellow

# Keep the job alive
do {
    $output = Receive-Job -Job $job -Keep
    if ($output) {
        # Show only the most recent lines to avoid spam
        $output | Select-Object -Last 1 | ForEach-Object { }
    }
    Start-Sleep -Seconds 1
} while ($job.State -eq "Running")

Write-Host ""
Write-Host "Application stopped." -ForegroundColor Green
Remove-Job -Job $job -Force
