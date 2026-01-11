# launch-web-test.ps1
# Launches web demo + isolated Chrome instance. Closing Chrome stops everything.

$projectPath = "Demo/Ai.Tlbx.VoiceAssistant.Demo.Web/Ai.Tlbx.VoiceAssistant.Demo.Web.csproj"
$url = "https://localhost:7079"

# Find Chrome
$chromePaths = @(
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
    "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
    "$env:LocalAppData\Google\Chrome\Application\chrome.exe"
)
$chromePath = $chromePaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $chromePath) {
    Write-Host "Chrome not found. Install Chrome or use: dotnet watch run --project $projectPath" -ForegroundColor Red
    exit 1
}

# Create temp profile directory (isolated Chrome instance, like VS does)
$tempProfile = Join-Path $env:TEMP "WebTestChrome_$([System.IO.Path]::GetRandomFileName())"

Write-Host "Starting web server..." -ForegroundColor Cyan

# Start dotnet
$dotnetProcess = Start-Process -FilePath "dotnet" `
    -ArgumentList "run", "--project", $projectPath, "--launch-profile", "https" `
    -PassThru -NoNewWindow

# Wait for server
Write-Host "Waiting for server..." -ForegroundColor Gray
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    Start-Sleep -Seconds 1
    if ($dotnetProcess.HasExited) {
        Write-Host "Server failed to start" -ForegroundColor Red
        exit 1
    }
    try {
        Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2 -SkipCertificateCheck -ErrorAction Stop | Out-Null
        $ready = $true
        break
    } catch {
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

if (-not $ready) {
    Write-Host "`nTimeout" -ForegroundColor Red
    $dotnetProcess.Kill($true)
    exit 1
}

Write-Host "`nLaunching Chrome (close it to stop server)..." -ForegroundColor Green

# Launch isolated Chrome instance
$chromeProcess = Start-Process -FilePath $chromePath -ArgumentList `
    "--user-data-dir=`"$tempProfile`"",
    "--no-first-run",
    "--no-default-browser-check",
    $url `
    -PassThru

# Wait for Chrome to close
$chromeProcess.WaitForExit()

Write-Host "Chrome closed. Stopping server..." -ForegroundColor Yellow

# Cleanup
$dotnetProcess.Kill($true)
$dotnetProcess.WaitForExit(5000)

# Remove temp profile
if (Test-Path $tempProfile) {
    Remove-Item -Path $tempProfile -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Done." -ForegroundColor Green
