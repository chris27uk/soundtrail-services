[CmdletBinding(PositionalBinding = $false)]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [string]$OutputDir = "$PSScriptRoot",
    [switch]$Restore,
    [switch]$Clean,
    [switch]$Publish,
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
# CI never runs AppHost; build the test project graph only (services + tests).
$BuildPath = if ($env:GITHUB_ACTIONS) { $TestsPath } else { $SolutionPath }

# Shippable apps (exclude AppHost / libraries / tests). Assembly versions stay pinned;
# deploy sets OTEL_SERVICE_VERSION from GitVersion / package manifest.
$PublishProjects = @(
    "src/Soundtrail.Services.Api/Soundtrail.Services.Api.csproj",
    "src/Soundtrail.Services.Enrichment.CatalogImport/Soundtrail.Services.Enrichment.CatalogImport.csproj",
    "src/Soundtrail.Services.Enrichment.Orchestrator/Soundtrail.Services.Enrichment.Orchestrator.csproj",
    "src/Soundtrail.Services.Enrichment.Scheduler/Soundtrail.Services.Enrichment.Scheduler.csproj",
    "src/Soundtrail.Services.Enrichment.Worker/Soundtrail.Services.Enrichment.Worker.csproj",
    "src/Soundtrail.Services.Projector/Soundtrail.Services.Projector.csproj"
)

# Output directories
$OutReporting = Join-Path $OutputDir "reports"
$OutPublish = Join-Path $OutputDir "artifacts/publish"
$OutPackage = Join-Path $OutputDir "package"

function Get-CiFastBuildProperties {
    if (-not $env:GITHUB_ACTIONS) {
        return @()
    }

    # Analyzers belong in the IDE / review, not every cold CI compile.
    return @(
        "/p:RunAnalyzersDuringBuild=false",
        "/p:RunAnalyzers=false",
        "/p:EnableNETAnalyzers=false",
        "/p:GenerateDocumentationFile=false",
        "/p:IsTransformWebConfigInHostBuild=false",
        "/p:UseSharedCompilation=true"
    )
}

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
            $BuildPath,
            "/p:Configuration=$Configuration",
            "--verbosity", "minimal"
        ) + (Get-CiFastBuildProperties)

        # CI must use committed lock files; local restore may update locks when packages change.
        # Do not pass /p:RestorePackagesWithLockFile=true here — Directory.Build.props enables it,
        # and AppHost opts out (Aspire injects host-RID packages that break cross-OS locked restore).
        if ($env:GITHUB_ACTIONS) {
            $restoreArgs += "--locked-mode"
        }

        Exec "dotnet" $restoreArgs
    }
}

function Get-CompileBuildProperties {
    # Version metadata is pinned in Directory.Build.props. Runtime SemVer is OTEL_SERVICE_VERSION.
    return @(
        "/p:RestoreDuringBuild=false"
    ) + (Get-CiFastBuildProperties)
}

# === Build ===
Invoke-Stage "Build Solution" {
    Write-Host "Build target: $BuildPath"
    Write-Host "Compile uses pinned assembly versions (runtime SemVer via OTEL_SERVICE_VERSION)"
    Exec "dotnet" (@(
        "build",
        $BuildPath,
        "/p:Configuration=$Configuration"
    ) + (Get-CompileBuildProperties) + @(
        "/maxcpucount:$MaxCpuCount",
        "--verbosity", "q",
        "--no-restore"
    ))
}

function ConvertTo-MtpTestFilterArgs {
    param(
        [string]$Filter
    )

    if ([string]::IsNullOrWhiteSpace($Filter)) {
        return @()
    }

    # VSTest-style filters used in docs/scripts — map to xUnit v3 MTP simple filters.
    if ($Filter -match 'FullyQualifiedName~EndToEnd') {
        return @('--filter-namespace', 'Soundtrail.Services.Tests.EndToEnd')
    }
    if ($Filter -match 'FullyQualifiedName~Integration') {
        return @('--filter-namespace', 'Soundtrail.Services.Tests.Integration')
    }
    if ($Filter -match 'FullyQualifiedName~Unit') {
        return @('--filter-namespace', 'Soundtrail.Services.Tests.Unit')
    }
    if ($Filter -match 'FullyQualifiedName~(?<name>.+)$') {
        $name = $Matches['name']
        if ($name -match '\.') {
            return @('--filter-class', $name)
        }
        return @('--filter-method', $name)
    }

    throw "Unsupported -TestFilter '$Filter'. Use FullyQualifiedName~EndToEnd|Integration|Unit or a class/method fragment."
}

# One test run via Microsoft Testing Platform (xUnit v3 executable).
Invoke-Stage "Run Tests" {
    $testArgs = @(
        "run", "--project", $TestsPath,
        "/p:Configuration=$Configuration",
        "--no-build",
        "--no-restore"
    ) + @(
        "--",
        "--report-xunit-trx",
        "--report-xunit-trx-filename", "tests.trx",
        "--results-directory", $OutReporting
    )

    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $testArgs += ConvertTo-MtpTestFilterArgs -Filter $TestFilter
    }

    $env:TESTINGPLATFORM_TELEMETRY_OPTOUT = "1"
    # Keep host/framework chatter out of CI so the MTP summary stays visible.
    # Dotted category names must use SetEnvironmentVariable (PowerShell $env: breaks on '.').
    $env:Logging__LogLevel__Default = "Warning"
    $env:Logging__LogLevel__Microsoft = "Warning"
    $env:Logging__LogLevel__System = "Warning"
    [Environment]::SetEnvironmentVariable("Logging__LogLevel__Microsoft.AspNetCore", "Warning", "Process")
    [Environment]::SetEnvironmentVariable("Logging__LogLevel__Microsoft.Hosting.Lifetime", "Warning", "Process")

    # Stream live (Exec buffers until exit and buries the summary under megabytes of host logs).
    Write-Host "> dotnet $($testArgs -join ' ')" -ForegroundColor Cyan
    & dotnet @testArgs
    $testExitCode = $LASTEXITCODE
    Write-TestRunSummary -TrxPath (Join-Path $OutReporting "tests.trx")
    if ($testExitCode -ne 0) {
        throw "Command failed with exit code ${testExitCode}: dotnet $($testArgs -join ' ')"
    }
}

# === Publish apps (packaging only; assembly versions stay pinned) ===
# Deploy must set OTEL_SERVICE_VERSION from package/version.txt (or GitVersion InformationalVersion).
if ($Publish) {
    Invoke-Stage "Publish Apps" {
        New-Item -ItemType Directory -Force -Path $OutPublish | Out-Null
        New-Item -ItemType Directory -Force -Path $OutPackage | Out-Null

        $otelServiceVersion = if (-not [string]::IsNullOrWhiteSpace($env:GITVERSION_INFORMATIONALVERSION)) {
            $env:GITVERSION_INFORMATIONALVERSION
        }
        elseif (-not [string]::IsNullOrWhiteSpace($env:OTEL_SERVICE_VERSION)) {
            $env:OTEL_SERVICE_VERSION
        }
        else {
            $Version
        }

        $versionManifest = [System.Collections.Generic.List[string]]::new()
        $versionManifest.Add("Version=$Version")
        $versionManifest.Add("OTEL_SERVICE_VERSION=$otelServiceVersion")
        $versionManifest.Add("# Deploy: set OTEL_SERVICE_VERSION on each service process from this value.")

        foreach ($relativeProject in $PublishProjects) {
            $projectPath = Join-Path $PSScriptRoot $relativeProject
            $projectName = [System.IO.Path]::GetFileNameWithoutExtension($relativeProject)
            $appOut = Join-Path $OutPublish $projectName

            Write-Host "Publishing $projectName -> $appOut"
            Exec "dotnet" (@(
                "publish",
                $projectPath,
                "/p:Configuration=$Configuration",
                "--output", $appOut,
                "/p:BuildProjectReferences=false"
            ) + (Get-CompileBuildProperties) + @(
                "/maxcpucount:$MaxCpuCount",
                "--verbosity", "q",
                "--no-restore"
            ))

            $dllPath = Join-Path $appOut "$projectName.dll"
            if (Test-Path $dllPath) {
                $versionManifest.Add("$projectName published")
                Write-Host "  published (assembly versions pinned; runtime SemVer via OTEL_SERVICE_VERSION=$otelServiceVersion)"
            }
            else {
                Write-Host "  warning: expected assembly not found at $dllPath"
            }
        }

        $manifestPath = Join-Path $OutPackage "publish-versions.txt"
        $versionManifest | Set-Content -Path $manifestPath -Encoding utf8
        # Same value deploy should inject as OTEL_SERVICE_VERSION.
        Set-Content -Path (Join-Path $OutPackage "version.txt") -Value $Version -Encoding utf8
        Write-Host "Published apps: $OutPublish"
        Write-Host "Version manifest: $manifestPath"
        Write-Host "OTEL_SERVICE_VERSION (deploy): $otelServiceVersion"
    }
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
    if ($Publish) {
        Write-Host "Published Apps: $OutPublish"
    }
    Write-TestRunSummary -TrxPath (Join-Path $OutReporting "tests.trx")
}
