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
$SolutionPath = Join-Path $PSScriptRoot "Soundtrail.Services.slnx"
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
# Skip verbose SDK dump in CI — it obscures stage timings and adds seconds on every invoke.
if (-not $env:GITHUB_ACTIONS) {
    Invoke-Stage "Environment Details" {
        Exec "dotnet" @("--info")
        Write-Host "Platform: $($IsWindows ? "Windows" : $IsMacOS ? "macOS" : "Linux")"
        Write-Host "Max CPU Count: $MaxCpuCount"
        Write-Host "Configuration: $Configuration"
        Write-Host "Version: $Version"
        if (-not [string]::IsNullOrWhiteSpace($env:GITVERSION_INFORMATIONALVERSION)) {
            Write-Host "InformationalVersion: $($env:GITVERSION_INFORMATIONALVERSION)"
        }
    }
}
else {
    Write-Host "CI build: Configuration=$Configuration Version=$Version MaxCpuCount=$MaxCpuCount"
    if (-not [string]::IsNullOrWhiteSpace($env:GITVERSION_INFORMATIONALVERSION)) {
        Write-Host "InformationalVersion: $($env:GITVERSION_INFORMATIONALVERSION)"
    }
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
# With -Restore, restore packages then continue into build/test (CI single-container path).
# Without -Restore, build uses --no-restore and expects a prior restore.
if ($Restore) {
    Invoke-Stage "NuGet Package Restore" {
        $restoreArgs = @(
            "restore",
            $SolutionPath,
            "/p:Configuration=$Configuration",
            "--verbosity", "minimal"
        )

        # CI must use committed lock files; local restore may update locks when packages change.
        # Do not pass /p:RestorePackagesWithLockFile=true here — Directory.Build.props enables it,
        # and AppHost opts out (Aspire injects host-RID packages that break cross-OS locked restore).
        if ($env:GITHUB_ACTIONS) {
            $restoreArgs += "--locked-mode"
        }

        Exec "dotnet" $restoreArgs
    }
}

function Get-VersionBuildProperties {
    param(
        [string]$BuildVersion
    )

    $properties = @(
        "/p:Version=$BuildVersion",
        # Belt-and-braces: never let MSBuild sneak a restore into compile.
        "/p:RestoreDuringBuild=false"
    )

    if (-not [string]::IsNullOrWhiteSpace($env:GITVERSION_INFORMATIONALVERSION)) {
        $properties += "/p:InformationalVersion=$($env:GITVERSION_INFORMATIONALVERSION)"
    }

    return $properties
}

# === Build ===
Invoke-Stage "Build Solution" {
    $versionProperties = Get-VersionBuildProperties -BuildVersion $Version
    Exec "dotnet" (@(
        "build",
        $SolutionPath,
        "/p:Configuration=$Configuration"
    ) + $versionProperties + @(
        "/maxcpucount:$MaxCpuCount",
        "--no-restore"
    ))
}

# One `dotnet test` so the runner (and xUnit, once parallelization is enabled) sees the full pack.
Invoke-Stage "Run Tests" {
    $versionProperties = Get-VersionBuildProperties -BuildVersion $Version
    $testArgs = @(
        "test", $TestsPath,
        "--logger", "trx;LogFileName=tests.trx",
        "--results-directory", $OutReporting,
        "/p:Configuration=$Configuration"
    ) + $versionProperties + @(
        "/maxcpucount:$MaxCpuCount",
        "--no-build",
        "--no-restore"
    )

    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $testArgs += @("--filter", $TestFilter)
    }

    Exec "dotnet" $testArgs
}

# === Build Summary ===
Invoke-Stage "Build Complete" {
    Write-Host "Build Summary" -ForegroundColor Cyan
    Write-Host "Version: $Version"
    if (-not [string]::IsNullOrWhiteSpace($env:GITVERSION_INFORMATIONALVERSION)) {
        Write-Host "InformationalVersion: $($env:GITVERSION_INFORMATIONALVERSION)"
    }
    Write-Host "Configuration: $Configuration"
    Write-Host "Elapsed: $($StopWatch.Elapsed.ToString())"
    Write-Host "Test Results: $OutReporting"
}
