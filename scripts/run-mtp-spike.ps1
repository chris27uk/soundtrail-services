param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $Root "tests/Soundtrail.Services.Tests.Mtp/Soundtrail.Services.Tests.Mtp.csproj"
$Reports = Join-Path $Root "reports"
$LogPath = Join-Path ([System.IO.Path]::GetTempPath()) "soundtrail-mtp-spike.txt"

New-Item -ItemType Directory -Force -Path $Reports | Out-Null
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

$env:TESTINGPLATFORM_TELEMETRY_OPTOUT = "1"

Write-Host "Building MTP spike project ($Configuration)..." -ForegroundColor Cyan
dotnet build $Project -c $Configuration 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    dotnet build $Project -c $Configuration
    exit $LASTEXITCODE
}

Write-Host "Running via dotnet run (MTP v2, xUnit v3)..." -ForegroundColor Cyan
Write-Host "Note: on .NET 10 SDK, MTP spike uses 'dotnet run' — not 'dotnet test' — so the main VSTest pack is unaffected." -ForegroundColor DarkGray

dotnet run --project $Project -c $Configuration --no-build -- `
    --results-directory $Reports `
    --report-xunit-trx `
    --report-xunit-trx-filename mtp-spike.trx

$exitCode = $LASTEXITCODE

Write-Host ""
Write-Host "Diagnostics log: $LogPath" -ForegroundColor Cyan
if (Test-Path $LogPath) {
    Get-Content $LogPath
}
else {
    Write-Host "(no diagnostics file written)"
}

$trxPath = Join-Path $Reports "mtp-spike.trx"
if (Test-Path $trxPath) {
    Write-Host ""
    Write-Host "TRX: $trxPath" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Compare with VSTest pack: pwsh ./build.ps1 -TestFilter 'FullyQualifiedName~EndToEnd'" -ForegroundColor DarkGray

exit $exitCode
