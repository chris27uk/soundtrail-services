[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [string]$OutputDir = "$PSScriptRoot",
    [switch]$Restore,
    [switch]$Clean,
    [int]$MaxCpuCount = 0,  # 0 = use all available cores
    [string]$TestFilter = ""
)

$StopWatch = [System.Diagnostics.StopWatch]::StartNew()
$MaxCpuCount = ($MaxCpuCount -eq 0) ? [Environment]::ProcessorCount : $MaxCpuCount
$ErrorActionPreference = "Stop"
Import-Module (Join-Path $PSScriptRoot "scripts/Functions.psm1")

# Reuse CI-provided NuGet cache path when available; otherwise fall back to local defaults.
$nugetPath = if ($env:NUGET_PACKAGES) {
    $env:NUGET_PACKAGES
} elseif ($IsWindows) {
    "C:\.nuget"
} else {
    Join-Path $HOME ".nuget/packages"
}
$env:NUGET_PACKAGES = $nugetPath
[Environment]::SetEnvironmentVariable("NUGET_PACKAGES", $nugetPath, "Process")
Write-Host "NuGet packages path: $nugetPath"

# Project paths
$SolutionPath = Join-Path $PSScriptRoot "Soundtrail.Services.sln"
$TestsPath = Join-Path $PSScriptRoot "tests/Soundtrail.Services.Tests/Soundtrail.Services.Tests.csproj"

# Output directories
$OutReporting = Join-Path $OutputDir "reports"

# === Ensure output directories exist ===
Invoke-Stage "Ensure Output Directories Exist" {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
    New-Item -ItemType Directory -Force -Path $OutReporting | Out-Null
    Write-Host "Created $OutReporting"
}

# === Environment info ===
Invoke-Stage "Environment Details" {
    Exec "dotnet" @("--info")
    Write-Host "Platform: $($IsWindows ? "Windows" : $IsMacOS ? "macOS" : "Linux")"
    Write-Host "Max CPU Count: $MaxCpuCount"
    Write-Host "Configuration: $Configuration"
    Write-Host "Version: $Version"
}

# === Clean ===
if ($Clean) {
    Invoke-Stage "Clean" {
        Exec "dotnet" @("clean", $SolutionPath, "/p:Configuration=$Configuration")
        Get-ChildItem $PSScriptRoot -Include bin, obj -Recurse -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "Removing $($_.FullName)"
            Remove-Item $_.FullName -Force -Recurse
        }
    }

    exit 0
}

# === Restore ===
if ($Restore) {
    Invoke-Stage "NuGet Package Restore" {
        Exec "dotnet" @(
            "restore",
            $SolutionPath,
            "/p:Configuration=$Configuration",
            "--verbosity", "normal"
        )
    }

    Write-Host "Restore complete..."
    exit 0
}

# === Build ===
Invoke-Stage "Build Solution" {
    Exec "dotnet" @(
        "build",
        $SolutionPath,
        "/p:Configuration=$Configuration",
        "/p:Version=$Version",
        "/maxcpucount:$MaxCpuCount",
        "--no-restore"
    )
}

function Invoke-TestStage {
    param(
        [string]$StageName,
        [string]$Filter,
        [string]$ResultFileName
    )

    Invoke-Stage $StageName {
        $testArgs = @(
            "test", $TestsPath,
            "--logger", "trx;LogFileName=$ResultFileName",
            "--results-directory", $OutReporting,
            "/p:Configuration=$Configuration",
            "/p:Version=$Version",
            "/maxcpucount:$MaxCpuCount",
            "--no-build",
            "--no-restore",
            "--filter", $Filter
        )

        Exec "dotnet" $testArgs
    }
}

if ([string]::IsNullOrWhiteSpace($TestFilter)) {
    Invoke-TestStage `
        -StageName "Run Unit Tests" `
        -Filter "FullyQualifiedName~Soundtrail.Services.Tests.Unit" `
        -ResultFileName "unit-tests.trx"

    Invoke-TestStage `
        -StageName "Run Integration Tests" `
        -Filter "FullyQualifiedName~Soundtrail.Services.Tests.Integration" `
        -ResultFileName "integration-tests.trx"
}
else {
    Invoke-TestStage `
        -StageName "Run Tests ($TestFilter)" `
        -Filter $TestFilter `
        -ResultFileName "tests.trx"
}

# === Build Summary ===
Invoke-Stage "Build Complete" {
    Write-Host "Build Summary" -ForegroundColor Cyan
    Write-Host "Version: $Version"
    Write-Host "Configuration: $Configuration"
    Write-Host "Elapsed: $($StopWatch.Elapsed.ToString())"
    Write-Host "Test Results: $OutReporting"
}
