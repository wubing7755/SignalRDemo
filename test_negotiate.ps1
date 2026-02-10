# SignalR Negotiate Endpoint Test Script
# 测试 SignalR negotiate 端点的简单脚本

param(
    [string]$ServerUrl = "http://localhost:5293",
    [string]$HubPath = "/chathub",
    [int]$WaitTime = 5
)

Write-Host "=== SignalR Negotiate Endpoint Test ===" -ForegroundColor Cyan
Write-Host "Server URL: $ServerUrl" -ForegroundColor Cyan
Write-Host "Hub Path: $HubPath" -ForegroundColor Cyan
Write-Host ""

# 清理现有进程
Write-Host "Stopping any existing server processes..." -ForegroundColor Yellow
Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -like "*SignalRDemo.Server*"
} | ForEach-Object {
    try {
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
    } catch {}
}
Start-Sleep -Seconds 2

# 构建项目
Write-Host "Building project..." -ForegroundColor Yellow
try {
    $buildResult = dotnet build "Server\SignalRDemo.Server.csproj" --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build successful" -ForegroundColor Green
} catch {
    Write-Host "Build error: $_" -ForegroundColor Red
    exit 1
}

# 启动服务器
Write-Host "Starting server..." -ForegroundColor Yellow
$serverProcess = Start-Process -NoNewWindow -PassThru -FilePath dotnet -ArgumentList "run", "--project", "Server\SignalRDemo.Server.csproj", "--no-build"

Write-Host "Waiting for server to start ($WaitTime seconds)..." -ForegroundColor Yellow
Start-Sleep -Seconds $WaitTime

# 测试 negotiate 端点
Write-Host "`nTesting negotiate endpoint..." -ForegroundColor Cyan
$negotiateUrl = "$ServerUrl$HubPath/negotiate"
Write-Host "URL: $negotiateUrl" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri $negotiateUrl -Method Post -ContentType "application/json" -Body "{}" -ErrorAction Stop
    Write-Host "✓ Negotiate endpoint responded successfully" -ForegroundColor Green

    if ($response.connectionId) {
        Write-Host "✓ ConnectionId: $($response.connectionId)" -ForegroundColor Green
    }

    if ($response.availableTransports) {
        Write-Host "✓ Available transports:" -ForegroundColor Green
        foreach ($transport in $response.availableTransports) {
            Write-Host "  - $($transport.transport)" -ForegroundColor Gray
        }
    }

    Write-Host "`nSignalR hub is working correctly!" -ForegroundColor Green

} catch [System.Net.WebException] {
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "✗ Negotiate endpoint not found (404)" -ForegroundColor Red
        Write-Host "Check that app.MapHub<ChatHub>('$HubPath') is configured in Program.cs" -ForegroundColor Yellow
    } elseif ($_.Exception.Response.StatusCode -eq 405) {
        Write-Host "✗ Method not allowed (405)" -ForegroundColor Red
        Write-Host "Try GET request instead of POST" -ForegroundColor Yellow
    } else {
        Write-Host "✗ Web error: $($_.Exception.Message)" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 清理
Write-Host "`nCleaning up..." -ForegroundColor Yellow
if ($serverProcess -and (-not $serverProcess.HasExited)) {
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Host "Server stopped" -ForegroundColor Green
}

Write-Host "`n=== Test completed ===" -ForegroundColor Cyan
