# SignalR Demo Server Test Script
# This script starts the server, tests connectivity, and stops it

param(
    [int]$TimeoutSeconds = 10,
    [string]$ServerUrl = "http://localhost:5293",
    [string]$HttpsUrl = "https://localhost:7002",
    [string]$ProjectPath = "Server\SignalRDemo.Server.csproj"
)

Write-Host "=== SignalR Demo Server Test ===" -ForegroundColor Cyan
Write-Host "Testing server at: $ServerUrl" -ForegroundColor Cyan
Write-Host "HTTPS endpoint: $HttpsUrl" -ForegroundColor Cyan
Write-Host ""

# Check if dotnet is available
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Dotnet not found"
    }
    Write-Host "✓ Dotnet version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Dotnet is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 6 SDK or higher" -ForegroundColor Yellow
    exit 1
}

# Check if project exists
if (-not (Test-Path $ProjectPath)) {
    Write-Host "✗ Project file not found: $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Project file found: $ProjectPath" -ForegroundColor Green

# Kill any existing dotnet processes for this project
Write-Host "`nStopping any existing server processes..." -ForegroundColor Yellow
$existingProcesses = Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -match "SignalRDemo.Server" -or $_.CommandLine -match $ProjectPath.Replace("\", "\\")
}
if ($existingProcesses) {
    foreach ($proc in $existingProcesses) {
        try {
            Write-Host "  Killing process PID: $($proc.Id)" -ForegroundColor Yellow
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        } catch {
            Write-Host "  Warning: Could not kill process $($proc.Id)" -ForegroundColor Yellow
        }
    }
    Start-Sleep -Seconds 2
}

# Build the project first
Write-Host "`nBuilding project..." -ForegroundColor Yellow
try {
    $buildOutput = dotnet build $ProjectPath 2>&1 | Out-String
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Build successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Build failed" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Build error: $_" -ForegroundColor Red
    exit 1
}

# Start the server
Write-Host "`nStarting server..." -ForegroundColor Yellow
$serverProcess = $null
try {
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = "dotnet"
    $processInfo.Arguments = "run --project $ProjectPath --no-build"
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.WorkingDirectory = (Get-Location).Path

    $serverProcess = New-Object System.Diagnostics.Process
    $serverProcess.StartInfo = $processInfo
    $serverProcess.EnableRaisingEvents = $true

    # Capture output
    $outputBuilder = New-Object System.Text.StringBuilder
    $errorBuilder = New-Object System.Text.StringBuilder

    $script:outputDataReceived = {
        if (-not [String]::IsNullOrEmpty($EventArgs.Data)) {
            [void]$outputBuilder.AppendLine($EventArgs.Data)
            Write-Host "[SERVER] $($EventArgs.Data)" -ForegroundColor DarkGray
        }
    }

    $script:errorDataReceived = {
        if (-not [String]::IsNullOrEmpty($EventArgs.Data)) {
            [void]$errorBuilder.AppendLine($EventArgs.Data)
            Write-Host "[SERVER ERROR] $($EventArgs.Data)" -ForegroundColor DarkRed
        }
    }

    $serverProcess.Add_OutputDataReceived($outputDataReceived)
    $serverProcess.Add_ErrorDataReceived($errorDataReceived)

    if ($serverProcess.Start()) {
        Write-Host "✓ Server process started (PID: $($serverProcess.Id))" -ForegroundColor Green
        $serverProcess.BeginOutputReadLine()
        $serverProcess.BeginErrorReadLine()

        # Wait for server to start
        Write-Host "`nWaiting for server to start (up to ${TimeoutSeconds}s)..." -ForegroundColor Yellow
        $startTime = Get-Date
        $serverReady = $false

        while (((Get-Date) - $startTime).TotalSeconds -lt $TimeoutSeconds) {
            try {
                # Test HTTP connection
                $response = Invoke-WebRequest -Uri $ServerUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
                if ($response.StatusCode -eq 200) {
                    $serverReady = $true
                    Write-Host "✓ Server is responding on HTTP (port 5293)" -ForegroundColor Green
                    break
                }
            } catch {
                # Server not ready yet
                Write-Host "." -NoNewline -ForegroundColor DarkGray
                Start-Sleep -Milliseconds 500
            }
        }

        if (-not $serverReady) {
            Write-Host "`n✗ Server did not start within $TimeoutSeconds seconds" -ForegroundColor Red
            throw "Server startup timeout"
        }

        # Test HTTPS if possible
        try {
            $httpsResponse = Invoke-WebRequest -Uri $HttpsUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop -SkipCertificateCheck
            if ($httpsResponse.StatusCode -eq 200) {
                Write-Host "✓ Server is responding on HTTPS (port 7002)" -ForegroundColor Green
            }
        } catch {
            Write-Host "⚠ HTTPS endpoint not accessible (may require certificate trust)" -ForegroundColor Yellow
        }

        # Test SignalR hub endpoint
        Write-Host "`nTesting SignalR hub..." -ForegroundColor Yellow
        try {
            $hubResponse = Invoke-WebRequest -Uri "$ServerUrl/chathub/negotiate" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            if ($hubResponse.StatusCode -eq 200) {
                Write-Host "✓ SignalR hub is accessible" -ForegroundColor Green
                $negotiateResult = $hubResponse.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
                if ($negotiateResult) {
                    Write-Host "  Negotiation response received" -ForegroundColor DarkGray
                }
            }
        } catch {
            Write-Host "⚠ SignalR hub negotiate endpoint not accessible" -ForegroundColor Yellow
        }

        Write-Host "`n=== Server Test Complete ===" -ForegroundColor Cyan
        Write-Host "✓ Server is running and accessible" -ForegroundColor Green
        Write-Host "  HTTP endpoint: $ServerUrl" -ForegroundColor Cyan
        Write-Host "  HTTPS endpoint: $HttpsUrl" -ForegroundColor Cyan
        Write-Host "  SignalR Hub: $ServerUrl/chathub" -ForegroundColor Cyan
        Write-Host "`nPress Enter to stop the server..." -ForegroundColor Yellow
        Read-Host

    } else {
        throw "Failed to start server process"
    }

} catch {
    Write-Host "`n✗ Error during server test: $_" -ForegroundColor Red
    Write-Host "Error details:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    if ($_.Exception.InnerException) {
        Write-Host "Inner exception: $($_.Exception.InnerException.Message)" -ForegroundColor Red
    }
} finally {
    # Clean up
    Write-Host "`nCleaning up..." -ForegroundColor Yellow
    if ($serverProcess -and (-not $serverProcess.HasExited)) {
        Write-Host "Stopping server process (PID: $($serverProcess.Id))..." -ForegroundColor Yellow
        try {
            # Try graceful shutdown first
            $serverProcess.CloseMainWindow() | Out-Null
            Start-Sleep -Seconds 2
            if (-not $serverProcess.HasExited) {
                Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
            }
            Write-Host "✓ Server stopped" -ForegroundColor Green
        } catch {
            Write-Host "⚠ Warning: Could not stop server process cleanly" -ForegroundColor Yellow
        }
    }

    # Kill any remaining dotnet processes
    $remainingProcesses = Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {
        $_.CommandLine -match "SignalRDemo.Server" -or $_.CommandLine -match $ProjectPath.Replace("\", "\\")
    }
    if ($remainingProcesses) {
        foreach ($proc in $remainingProcesses) {
            try {
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            } catch { }
        }
    }
}

Write-Host "`n=== Test Script Finished ===" -ForegroundColor Cyan
