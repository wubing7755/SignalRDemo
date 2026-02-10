# SignalR Chat Application Test Script
# This script tests the SignalR chat application by starting the server and checking for common errors

param(
    [int]$TimeoutSeconds = 15,
    [string]$ServerHttpUrl = "http://localhost:5293",
    [string]$ServerHttpsUrl = "https://localhost:7002",
    [string]$HubPath = "/chathub",
    [switch]$NoBuild
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "SignalR Chat Application Test" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Function to write colored output
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Gray
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Step 1: Clean up existing processes
Write-Info "Step 1: Cleaning up existing server processes..."
$existingProcesses = Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -match "SignalRDemo.Server" -or $_.CommandLine -match "Server\\SignalRDemo.Server.csproj"
}

if ($existingProcesses.Count -gt 0) {
    Write-Warning "Found $($existingProcesses.Count) existing dotnet process(es)"
    foreach ($proc in $existingProcesses) {
        try {
            Write-Info "Stopping process PID: $($proc.Id)"
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        } catch {
            Write-Warning "Could not stop process $($proc.Id): $_"
        }
    }
    Start-Sleep -Seconds 2
}

# Step 2: Build the project (if not skipped)
if (-not $NoBuild) {
    Write-Info "Step 2: Building the project..."
    try {
        $buildOutput = dotnet build 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Build completed successfully"
        } else {
            Write-Error "Build failed with exit code $LASTEXITCODE"
            Write-Host $buildOutput -ForegroundColor Red
            exit 1
        }
    } catch {
        Write-Error "Build error: $_"
        exit 1
    }
} else {
    Write-Info "Step 2: Skipping build (NoBuild switch specified)"
}

# Step 3: Start the server
Write-Info "Step 3: Starting the server..."
$serverProcess = $null

try {
    # Start server process in background
    $processInfo = New-Object System.Diagnostics.ProcessStartInfo
    $processInfo.FileName = "dotnet"
    $processInfo.Arguments = "run --project Server\SignalRDemo.Server.csproj --no-build"
    $processInfo.RedirectStandardOutput = $true
    $processInfo.RedirectStandardError = $true
    $processInfo.UseShellExecute = $false
    $processInfo.CreateNoWindow = $true
    $processInfo.WorkingDirectory = $PWD.Path

    $serverProcess = New-Object System.Diagnostics.Process
    $serverProcess.StartInfo = $processInfo

    # Capture output for debugging
    $outputBuilder = New-Object System.Text.StringBuilder
    $errorBuilder = New-Object System.Text.StringBuilder

    $scriptBlockOutput = {
        if (-not [String]::IsNullOrEmpty($EventArgs.Data)) {
            [void]$outputBuilder.AppendLine($EventArgs.Data)
        }
    }

    $scriptBlockError = {
        if (-not [String]::IsNullOrEmpty($EventArgs.Data)) {
            [void]$errorBuilder.AppendLine($EventArgs.Data)
        }
    }

    Register-ObjectEvent -InputObject $serverProcess -EventName OutputDataReceived -Action $scriptBlockOutput | Out-Null
    Register-ObjectEvent -InputObject $serverProcess -EventName ErrorDataReceived -Action $scriptBlockError | Out-Null

    if ($serverProcess.Start()) {
        $serverProcess.BeginOutputReadLine()
        $serverProcess.BeginErrorReadLine()
        Write-Success "Server process started (PID: $($serverProcess.Id))"
    } else {
        throw "Failed to start server process"
    }
} catch {
    Write-Error "Failed to start server: $_"
    exit 1
}

# Step 4: Wait for server to start
Write-Info "Step 4: Waiting for server to start (timeout: ${TimeoutSeconds}s)..."
$serverReady = $false
$startTime = Get-Date

while (((Get-Date) - $startTime).TotalSeconds -lt $TimeoutSeconds) {
    try {
        # Test HTTP connection
        $response = Invoke-WebRequest -Uri $ServerHttpUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $serverReady = $true
            Write-Success "Server is responding on HTTP ($ServerHttpUrl)"
            break
        }
    } catch {
        # Check if server process is still running
        if ($serverProcess.HasExited) {
            Write-Error "Server process exited unexpectedly"
            Write-Host "Standard Output:" -ForegroundColor Red
            Write-Host $outputBuilder.ToString() -ForegroundColor Red
            Write-Host "Standard Error:" -ForegroundColor Red
            Write-Host $errorBuilder.ToString() -ForegroundColor Red
            exit 1
        }

        # Server not ready yet
        Write-Host "." -NoNewline -ForegroundColor DarkGray
        Start-Sleep -Milliseconds 500
    }
}

if (-not $serverReady) {
    Write-Error "Server did not start within $TimeoutSeconds seconds"
    Write-Host "Server output:" -ForegroundColor Yellow
    Write-Host $outputBuilder.ToString() -ForegroundColor Yellow
    Write-Host "Server errors:" -ForegroundColor Yellow
    Write-Host $errorBuilder.ToString() -ForegroundColor Yellow

    # Try to get more detailed error info
    Write-Info "Checking for common errors in server output..."

    $outputText = $outputBuilder.ToString()
    $errorText = $errorBuilder.ToString()

    if ($outputText -match "System\.PlatformNotSupportedException" -or $errorText -match "System\.PlatformNotSupportedException") {
        Write-Error "DETECTED: PlatformNotSupportedException"
        Write-Host "This error often occurs in Blazor WebAssembly when trying to use" -ForegroundColor Red
        Write-Host "X509 certificates or other platform-specific features." -ForegroundColor Red
        Write-Host "" -ForegroundColor Red
        Write-Host "Common causes in SignalR:" -ForegroundColor Yellow
        Write-Host "1. SignalR client trying to validate HTTPS certificates in WebAssembly" -ForegroundColor Yellow
        Write-Host "2. Using HTTP message handlers that trigger certificate operations" -ForegroundColor Yellow
        Write-Host "" -ForegroundColor Yellow
        Write-Host "Solutions to try:" -ForegroundColor Green
        Write-Host "1. Use HTTP instead of HTTPS for WebAssembly development" -ForegroundColor Green
        Write-Host "2. Ensure ChatService uses minimal configuration without HttpMessageHandlerFactory" -ForegroundColor Green
        Write-Host "3. Check that SignalR client is configured correctly for WebAssembly" -ForegroundColor Green
    }

    if ($outputText -match "X509" -or $errorText -match "X509") {
        Write-Error "DETECTED: X509 certificate related error"
        Write-Host "WebAssembly does not support System.Security.Cryptography.X509Certificates" -ForegroundColor Red
    }

    # Stop server process
    if (-not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    }

    exit 1
}

# Step 5: Test SignalR hub
Write-Info "Step 5: Testing SignalR hub connection..."
$hubTestUrl = "${ServerHttpUrl}${HubPath}/negotiate"

try {
    Write-Info "Testing SignalR negotiate endpoint: $hubTestUrl"
    $negotiateResponse = Invoke-RestMethod -Uri $hubTestUrl -Method Post -ContentType "application/json" -Body "{}" -TimeoutSec 5 -ErrorAction Stop

    if ($negotiateResponse.connectionId) {
        Write-Success "SignalR hub is accessible"
        Write-Info "ConnectionId: $($negotiateResponse.connectionId)"

        if ($negotiateResponse.availableTransports) {
            Write-Info "Available transports:"
            foreach ($transport in $negotiateResponse.availableTransports) {
                Write-Info "  - $($transport.transport)"
            }
        }
    } else {
        Write-Warning "SignalR negotiate succeeded but no connectionId returned"
    }
} catch {
    Write-Error "Failed to connect to SignalR hub: $_"

    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Error "SignalR hub endpoint not found (404)"
        Write-Host "Check that the following is configured in Server/Program.cs:" -ForegroundColor Yellow
        Write-Host "1. app.MapHub<ChatHub>('$HubPath')" -ForegroundColor Yellow
        Write-Host "2. builder.Services.AddSignalR()" -ForegroundColor Yellow
        Write-Host "3. Proper CORS configuration for Blazor client" -ForegroundColor Yellow
    } elseif ($_.Exception.Response.StatusCode -eq 405) {
        Write-Error "Method not allowed (405) for negotiate endpoint"
        Write-Host "Try changing the negotiate test to use GET method" -ForegroundColor Yellow
    }
}

# Step 6: Test Blazor WebAssembly client configuration
Write-Info "Step 6: Checking client configuration for WebAssembly compatibility..."
Write-Info "Checking ChatService configuration..."

$chatServicePath = "Client\Services\ChatService.cs"
if (Test-Path $chatServicePath) {
    $chatServiceContent = Get-Content $chatServicePath -Raw

    # Check for problematic patterns
    $issuesFound = @()

    if ($chatServiceContent -match "HttpMessageHandlerFactory") {
        $issuesFound += "HttpMessageHandlerFactory - Can trigger X509 issues in WebAssembly"
    }

    if ($chatServiceContent -match "X509" -or $chatServiceContent -match "Certificate") {
        $issuesFound += "X509/Certificate references - Not supported in WebAssembly"
    }

    if ($chatServiceContent -match "WithAutomaticReconnect") {
        $issuesFound += "WithAutomaticReconnect - May not be available in .NET 6 SignalR 1.1.0"
    }

    if ($issuesFound.Count -gt 0) {
        Write-Warning "Potential issues found in ChatService.cs:"
        foreach ($issue in $issuesFound) {
            Write-Host "  - $issue" -ForegroundColor Yellow
        }
        Write-Host ""
        Write-Host "Recommended ChatService configuration for WebAssembly:" -ForegroundColor Green
        Write-Host "1. Use simplest configuration: .WithUrl(hubUrl) only" -ForegroundColor Green
        Write-Host "2. Avoid HttpMessageHandlerFactory configuration" -ForegroundColor Green
        Write-Host "3. Use HTTP (not HTTPS) for WebAssembly development" -ForegroundColor Green
    } else {
        Write-Success "ChatService.cs appears to be properly configured for WebAssembly"
    }
} else {
    Write-Error "ChatService.cs not found at $chatServicePath"
}

# Step 7: Summary
Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Success "Server is running and accessible"
Write-Info "HTTP endpoint: $ServerHttpUrl"
Write-Info "HTTPS endpoint: $ServerHttpsUrl"
Write-Info "SignalR Hub: ${ServerHttpUrl}${HubPath}"
Write-Host ""
Write-Info "To test the chat application:"
Write-Host "1. Open browser and navigate to: $ServerHttpUrl" -ForegroundColor Cyan
Write-Host "2. Click on the '聊天室' link in the navigation menu" -ForegroundColor Cyan
Write-Host "3. Open another browser window/tab to test multi-user chat" -ForegroundColor Cyan
Write-Host ""
Write-Info "Press Ctrl+C to stop the server and exit this script"
Write-Host ""

# Keep script running and wait for user interrupt
try {
    while ($true) {
        if ($serverProcess.HasExited) {
            Write-Error "Server process exited unexpectedly"
            break
        }
        Start-Sleep -Seconds 1
    }
} finally {
    # Cleanup
    Write-Info "Cleaning up..."
    if ($serverProcess -and (-not $serverProcess.HasExited)) {
        Write-Info "Stopping server process..."
        Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    }

    # Unregister events
    Get-EventSubscriber | Where-Object {$_.SourceIdentifier -like "*"} | Unregister-Event -ErrorAction SilentlyContinue

    Write-Host ""
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Success "Test script completed"
    Write-Host "===============================================" -ForegroundColor Cyan
}
