# SignalR Hub Test Script
# This script tests the SignalR hub negotiate endpoint

param(
    [string]$ServerUrl = "http://localhost:5293",
    [string]$HubPath = "/chathub",
    [int]$TimeoutSeconds = 10
)

Write-Host "=== SignalR Hub Test ===" -ForegroundColor Cyan
Write-Host "Testing SignalR hub at: $ServerUrl$HubPath" -ForegroundColor Cyan
Write-Host ""

# Function to test HTTP endpoint
function Test-HttpEndpoint {
    param(
        [string]$Url,
        [string]$Method = "GET"
    )

    try {
        Write-Host "Testing $Method $Url..." -ForegroundColor Yellow
        $response = Invoke-WebRequest -Uri $Url -Method $Method -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop

        Write-Host "✓ Response status: $($response.StatusCode)" -ForegroundColor Green

        # Try to parse JSON response
        try {
            $json = $response.Content | ConvertFrom-Json
            Write-Host "✓ JSON response received:" -ForegroundColor Green
            $json | Format-List | Out-String | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
            return $true, $response
        } catch {
            Write-Host "  Response content (not JSON): $($response.Content)" -ForegroundColor DarkGray
            return $true, $response
        }
    } catch [System.Net.WebException] {
        Write-Host "✗ WebException: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            Write-Host "  Status code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        }
        return $false, $null
    } catch {
        Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
        return $false, $null
    }
}

# Function to test SignalR negotiate endpoint
function Test-SignalRNegotiate {
    param(
        [string]$BaseUrl,
        [string]$HubPath
    )

    $negotiateUrl = "$BaseUrl$HubPath/negotiate"
    Write-Host "`n=== Testing SignalR Negotiate Endpoint ===" -ForegroundColor Cyan
    Write-Host "Negotiate URL: $negotiateUrl" -ForegroundColor Cyan

    $success, $response = Test-HttpEndpoint -Url $negotiateUrl

    if ($success -and $response) {
        # Check for expected SignalR negotiate response
        try {
            $json = $response.Content | ConvertFrom-Json -ErrorAction Stop

            # SignalR negotiate response should contain connectionId and availableTransports
            $hasConnectionId = $null -ne $json.connectionId
            $hasAvailableTransports = $null -ne $json.availableTransports

            if ($hasConnectionId) {
                Write-Host "✓ SignalR negotiate successful!" -ForegroundColor Green
                Write-Host "  ConnectionId: $($json.connectionId)" -ForegroundColor Cyan

                if ($hasAvailableTransports) {
                    Write-Host "  Available transports:" -ForegroundColor Cyan
                    foreach ($transport in $json.availableTransports) {
                        Write-Host "    - $($transport.transport)" -ForegroundColor DarkGray
                    }
                }

                return $true, $json
            } else {
                Write-Host "⚠ Response doesn't look like SignalR negotiate (missing connectionId)" -ForegroundColor Yellow
                return $false, $json
            }
        } catch {
            Write-Host "⚠ Response is not valid JSON" -ForegroundColor Yellow
            return $false, $null
        }
    } else {
        Write-Host "✗ Failed to access negotiate endpoint" -ForegroundColor Red
        return $false, $null
    }
}

# Function to test CORS configuration
function Test-CorsConfiguration {
    param(
        [string]$Url
    )

    Write-Host "`n=== Testing CORS Configuration ===" -ForegroundColor Cyan

    try {
        # Send OPTIONS request to check CORS headers
        $optionsResponse = Invoke-WebRequest -Uri $Url -Method OPTIONS -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop

        Write-Host "✓ OPTIONS request successful" -ForegroundColor Green

        # Check for CORS headers
        $corsHeaders = @("Access-Control-Allow-Origin", "Access-Control-Allow-Methods", "Access-Control-Allow-Headers", "Access-Control-Allow-Credentials")
        $foundHeaders = @()

        foreach ($header in $corsHeaders) {
            $headerValue = $optionsResponse.Headers[$header]
            if ($headerValue) {
                $foundHeaders += "$header: $headerValue"
                Write-Host "  ✓ $header: $headerValue" -ForegroundColor Green
            } else {
                Write-Host "  ⚠ $header: Not found" -ForegroundColor Yellow
            }
        }

        if ($foundHeaders.Count -gt 0) {
            Write-Host "✓ CORS headers detected" -ForegroundColor Green
            return $true
        } else {
            Write-Host "⚠ No CORS headers found" -ForegroundColor Yellow
            return $false
        }

    } catch {
        Write-Host "⚠ OPTIONS request failed: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

# Function to test hub base URL
function Test-HubBaseUrl {
    param(
        [string]$Url
    )

    Write-Host "`n=== Testing Hub Base URL ===" -ForegroundColor Cyan

    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        Write-Host "✓ Hub base URL accessible" -ForegroundColor Green
        Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Cyan

        # SignalR hub base URL might return 200 OK or might have special handling
        if ($response.Content -match "WebSocket" -or $response.Content -match "SignalR") {
            Write-Host "  Content suggests SignalR endpoint" -ForegroundColor Cyan
        }

        return $true
    } catch [System.Net.WebException] {
        if ($_.Exception.Response.StatusCode -eq 404) {
            Write-Host "✓ Hub base URL returns 404 (expected for SignalR hub)" -ForegroundColor Green
            return $true
        } elseif ($_.Exception.Response.StatusCode -eq 400) {
            Write-Host "✓ Hub base URL returns 400 (expected - requires WebSocket connection)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ Hub base URL error: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ Hub base URL error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Main test execution
try {
    # Test basic server connectivity
    Write-Host "=== Testing Server Connectivity ===" -ForegroundColor Cyan
    $serverTest = Test-HttpEndpoint -Url $ServerUrl

    if (-not $serverTest[0]) {
        Write-Host "`n✗ Cannot connect to server. Please make sure the server is running." -ForegroundColor Red
        Write-Host "  Run command: dotnet run --project Server\SignalRDemo.Server.csproj" -ForegroundColor Yellow
        exit 1
    }

    # Test hub base URL
    $hubUrl = "$ServerUrl$HubPath"
    Test-HubBaseUrl -Url $hubUrl

    # Test negotiate endpoint
    $negotiateSuccess, $negotiateResponse = Test-SignalRNegotiate -BaseUrl $ServerUrl -HubPath $HubPath

    # Test CORS configuration
    Test-CorsConfiguration -Url $hubUrl

    # Summary
    Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
    if ($negotiateSuccess) {
        Write-Host "✓ SignalR hub is configured and accessible" -ForegroundColor Green
        Write-Host "  Negotiate endpoint: $ServerUrl$HubPath/negotiate" -ForegroundColor Cyan
        Write-Host "  WebSocket endpoint: ws://localhost:5293$HubPath" -ForegroundColor Cyan
        Write-Host "  (or wss://localhost:7002$HubPath for HTTPS)" -ForegroundColor Cyan
    } else {
        Write-Host "⚠ SignalR hub may not be properly configured" -ForegroundColor Yellow
        Write-Host "  Possible issues:" -ForegroundColor Yellow
        Write-Host "  1. Hub route not registered in Program.cs" -ForegroundColor Yellow
        Write-Host "  2. CORS configuration issue" -ForegroundColor Yellow
        Write-Host "  3. Missing SignalR middleware configuration" -ForegroundColor Yellow

        Write-Host "`n  Check Server\Program.cs for:" -ForegroundColor Yellow
        Write-Host "    - app.MapHub<ChatHub>(\"/chathub\")" -ForegroundColor Yellow
        Write-Host "    - builder.Services.AddSignalR()" -ForegroundColor Yellow
        Write-Host "    - app.UseCors() configuration" -ForegroundColor Yellow
    }

} catch {
    Write-Host "`n✗ Unexpected error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.Exception.StackTrace)" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Test Completed ===" -ForegroundColor Cyan
